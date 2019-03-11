using logviewer.Interfaces;
using logviewer.query.Aggregates;
using logviewer.query.Interfaces;
using logviewer.query.Nodes;
using logviewer.query.Parsing;
using logviewer.query.Visitors;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace logviewer.query
{
    /// <summary>
    /// A class for executing queries on an indexed log provided by a <see cref="BaseLog"/> instance.
    /// </summary>
    internal static class QueryFactory
    {
        #region query settings

        /// <summary>
        /// The format used for parsing timestamps
        /// </summary>
        public static string DateTimeFormat;

        /// <summary>
        /// If more than the given percentage of the log items are returned during indexing, the step is skipped and the query executes a full scan as it saves the time for indexing
        /// </summary>
        public static double IndexSkipFactor = 0.7;

        #endregion

        #region query parser

        private static bool _dynamicFields;
        private static IDictionary<string, Type> _fieldTypes = new Dictionary<string, Type>();

        // variables
        internal static readonly ParameterExpression ItemVariable = Expression.Variable(typeof(LogItem), "item");
        private static readonly ParameterExpression StringParameter = Expression.Parameter(typeof(string), "message");
        private static readonly ParameterExpression FileParameter = Expression.Parameter(typeof(string), "file");
        private static readonly ParameterExpression LineParameter = Expression.Parameter(typeof(int), "line");
        private static readonly ParameterExpression ObjectVariable = Expression.Variable(typeof(object), "object");

        #region expressions

        // literals
        private static readonly Parser<string> String = (Parse.Char('\\').Then(c => Parse.AnyChar)).Or(Parse.AnyChar.Except(Parse.Char('"'))).Many().Text().Contained(Parse.Char('"'), Parse.Char('"'));
        private static readonly Parser<double?> Constant = Parse.DecimalInvariant.Select(x => new Nullable<double>(double.Parse(x))).Named("number");
        private static readonly Parser<string> FieldName =
            Parse.XDelimitedBy(Parse.Identifier(Parse.Letter, Parse.LetterOrDigit), Parse.Char('.')).Select(e => string.Join(".", e))
            .Or(Parse.Contained(Parse.AnyChar.Except(Parse.Char('`')).Many().Text(), Parse.Char('`'), Parse.Char('`')));

        // operators
        private static readonly Parser<ExpressionType> Add          = Parse.String("+").Token().Return(ExpressionType.AddChecked);
        private static readonly Parser<ExpressionType> Subtract     = Parse.String("-").Token().Return(ExpressionType.SubtractChecked);
        private static readonly Parser<ExpressionType> Multiply     = Parse.String("*").Token().Return(ExpressionType.MultiplyChecked);
        private static readonly Parser<ExpressionType> Divide       = Parse.String("/").Token().Return(ExpressionType.Divide);
        private static readonly Parser<ExpressionType> Modulo       = Parse.String("%").Token().Return(ExpressionType.Modulo);
        private static readonly Parser<ExpressionType> Power        = Parse.String("^").Token().Return(ExpressionType.Power);
        private static readonly Parser<ExpressionType> And          = Parse.String("and").Return(ExpressionType.AndAlso);
        private static readonly Parser<ExpressionType> Or           = Parse.String("or").Return(ExpressionType.OrElse);
        private static readonly Parser<ExpressionType> XOr          = Parse.String("xor").Return(ExpressionType.ExclusiveOr);
        private static readonly Parser<ExpressionType> Not          = Parse.String("not").Return(ExpressionType.Not);
        private static readonly Parser<ExpressionType> Equal        = Parse.String("==").Token().Return(ExpressionType.Equal);
        private static readonly Parser<ExpressionType> NotEqual     = Parse.String("!=").Token().Return(ExpressionType.NotEqual);
        private static readonly Parser<ExpressionType> Greater      = Parse.String(">").Token().Return(ExpressionType.GreaterThan);
        private static readonly Parser<ExpressionType> GreaterEqual = Parse.String(">=").Token().Return(ExpressionType.GreaterThanOrEqual);
        private static readonly Parser<ExpressionType> Less         = Parse.String("<").Token().Return(ExpressionType.LessThan);
        private static readonly Parser<ExpressionType> LessEqual    = Parse.String("<=").Token().Return(ExpressionType.LessThanOrEqual);

        // literal expressions
        private static readonly Parser<Expression> StringExpression = String.Select(v => Expression.Constant(v));
        private static readonly Parser<Expression> ConstantExpression = Constant.Select(v => Expression.Constant(v, typeof(double?)));
        private static readonly Parser<Expression> TrueExpression = Parse.String("true").Token().Select(_ => Expression.Constant(true));
        private static readonly Parser<Expression> FalseExpression = Parse.String("false").Token().Select(_ => Expression.Constant(false));

        // log item property values
        private static readonly MethodInfo GetValueMethod = typeof(LogItem).GetMethod("GetValue", BindingFlags.Instance | BindingFlags.Public);
        private static readonly Parser<Expression> ExpressionProperty =
             FieldName
            .Where(name => _dynamicFields || _fieldTypes.ContainsKey(name))
            .Select(name =>
            {
                var value = (Expression)Expression.Call(ItemVariable, GetValueMethod, Expression.Constant(name));

                if (_fieldTypes.ContainsKey(name))
                {
                    value = Expression.Convert(value, _fieldTypes[name].IsValueType && (!_fieldTypes[name].IsGenericType || _fieldTypes[name].GetGenericTypeDefinition() != typeof(Nullable<>)) ? typeof(Nullable<>).MakeGenericType(_fieldTypes[name]) : _fieldTypes[name]);
                }

                return value;
            });

        // function calls
        private static readonly Parser<Expression> ExpressionFunction =
            from name in Parse.Identifier(Parse.Letter, Parse.LetterOrDigit)
            from lparen in Parse.Char('(')
            from expr in Parse.Ref(() => ExpressionParser).DelimitedBy(Parse.Char(',').Token())
            from rparen in Parse.Char(')')
            select CreateMethodCall(name, expr);
        private static Expression CreateMethodCall(string name, IEnumerable<Expression> expr)
        {
            var method = typeof(QueryFunctions).GetMethod(name, expr.Select(e => e.Type).ToArray());
            if (method == null)
            {
                throw new ParseException($"Undefined method '{name}'");
            }

            return Expression.Call(method, expr.ToArray());
        }

        // expression tree
        private static readonly Parser<Expression> ExpressionFactor =
            (from lparen in Parse.Char('(')
             from expr in Parse.Ref(() => ExpressionParser)
             from rparen in Parse.Char(')')
             select expr).Named("expression")
             .Or(TrueExpression)
             .Or(FalseExpression)
             .Or(ConstantExpression)
             .Or(StringExpression)
             .Or(ExpressionFunction)
             .Or(ExpressionProperty);

        private static readonly Parser<Expression> ExpressionOperand =
            ((from sign in Sprache.Parse.Char('-')
              from factor in ExpressionFactor
              select Expression.Negate(factor)
             ).XOr(ExpressionFactor)).Token();

        private static BinaryExpression MakeBinary(ExpressionType type, Expression left, Expression right)
        {
            if (left.Type == typeof(object))
            {
                left = Expression.ConvertChecked(left, right.Type);
            }
            else if (right.Type == typeof(object))
            {
                right = Expression.ConvertChecked(right, left.Type);
            }

            return Expression.MakeBinary(type, left, right);
        }

        private static readonly Parser<Expression> ExpressionPower = Parse.ChainOperator(Power, ExpressionOperand, MakeBinary);
        private static readonly Parser<Expression> ExpressionMulDivMod = Parse.ChainOperator(Multiply.Or(Divide).Or(Modulo), ExpressionPower, MakeBinary);
        private static readonly Parser<Expression> ExpressionAddSub = Parse.ChainOperator(Add.Or(Subtract), ExpressionMulDivMod, MakeBinary);
        private static readonly Parser<Expression> ExpressionRelational = Parse.ChainOperator(Greater.Or(GreaterEqual).Or(Less).Or(LessEqual), ExpressionAddSub, MakeBinary);
        private static readonly Parser<Expression> ExpressionEquality = Parse.ChainOperator(Equal.Or(NotEqual), ExpressionRelational, MakeBinary);
        private static readonly Parser<Expression> ExpressionAnd = Parse.ChainOperator(And, ExpressionEquality, MakeBinary);
        private static readonly Parser<Expression> ExpressionParser = Parse.ChainOperator(Or.Or(XOr), ExpressionAnd, MakeBinary);

        #endregion

        #region searches

        private static readonly Parser<MatchNode> SearchPhraseParser =
            from exact in Parse.Char('!').Optional().Select(o => !o.IsEmpty)
            from openingQuot in Parse.Char('"')
            from pattern in (Parse.Char('\\').Then(c => Parse.AnyChar)).Or(Parse.AnyChar.Except(Parse.Char('"'))).Many().Text()
            from closingQuot in Parse.Char('"')
            from message in Parse.Char(':').Then(_ => FieldName.Token().Then(field => Parse.Char('=').Token().Then(__ => String.Select(value => new Tuple<string, string>(field, value))))).XOptional()
            select new PhraseNode(pattern, exact, message.GetOrDefault()?.Item1, message.GetOrDefault()?.Item2);
        
        private static readonly Parser<MatchNode> SearchFactorParser =
            (from lparen in Sprache.Parse.Char('(')
             from expr in Sprache.Parse.Ref(() => Search)
             from rparen in Sprache.Parse.Char(')')
             select expr).Or(SearchPhraseParser).Or(Parse.Char('*').Token().Return(new ScanNode()));

        private static readonly Parser<MatchNode> SearchOperandParser =
            ((from sign in Parse.String("not").Token()
              from factor in SearchFactorParser
              select new NotNode(factor)
             ).Or(SearchFactorParser)).Token();

        private static readonly Parser<MatchNode> Search = Parse.ChainOperator(Or, Parse.ChainOperator(And, SearchOperandParser, Node.MakeBinary), Node.MakeBinary);

        #endregion
        
        #region grouping

        private static readonly Parser<Tuple<string, Expression>> GroupParser =
            from term in ExpressionParser.Captured()
            from name in Parse.String("as").Token().Then(_ => FieldName.Or(String)).XOptional()
            select new Tuple<string, Expression>(name.GetOrElse(term.Text).Trim(), term.Value);

        #endregion

        #region aggregates

        private static readonly Parser<IAggregate> AggregateFunctionParser =
            from type in Parse.Identifier(Parse.Letter, Parse.LetterOrDigit)
            from lparen in Parse.Char('(')
            from expression in ExpressionParser.Or(Parse.Char('*').Token().Select(s => Expression.Constant(s))).DelimitedBy(Parse.Char(',').Token()).Where(i => i.Count() > 0)
            from rparen in Parse.Char(')')
            let aggregate = CreateAggregate(type, expression)
            where aggregate != null
            select aggregate;

        private static readonly Parser<Tuple<string, IAggregate>> AggregateParser =
            from agg in AggregateFunctionParser.Or(ExpressionParser.Select(e => new ListAggregate(new[] { e }))).Captured()
            from name in Parse.String("as").Token().Then(_ => FieldName.Or(String)).Optional()
            select new Tuple<string, IAggregate>(name.GetOrElse(agg.Text).Trim(), agg.Value);

        #endregion

        #region ordering

        private static readonly Parser<Tuple<string, bool>> OrderColumnParser =
            from column in FieldName.Or(String)
            from direction in Parse.String("asc").Token().Return(false).Or(Parse.String("desc").Token().Return(true)).XOptional()
            select new Tuple<string, bool>(column.Trim(), direction.GetOrElse(false));

        #endregion

        private static readonly Parser<Node> QueryParser = (
            from match in Search.Select(m => { var v = new FieldsVisitor(new List<string>()); m.Accept(v); _fieldTypes = v.Fields; return m; })
            from filter in Parse.String("where").Token().Then(_ => ExpressionParser).XOptional()
            from grouping in Parse.String("group").Token().Concat(Parse.String("by").Token()).Then(_ => GroupParser.DelimitedBy(Parse.Char(',').Token())).XOptional()
            from selects in Parse.String("select").Token().Then(_ => AggregateParser.DelimitedBy(Parse.Char(',').Token())).XOptional()
            from order in Parse.String("order").Token().Concat(Parse.String("by").Token()).Then(_ => OrderColumnParser.DelimitedBy(Parse.Char(',').Token())).XOptional()
            from limit in Parse.String("limit").Token().Then(_ => Constant.Select(c => (int)c)).XOptional()
            select CreateQueryTree(match, filter, grouping, selects, order, limit)).End();

        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="Query"/> class.
        /// </summary>
        /// <param name="log">The log to query</param>
        /// <param name="settings">The application settings</param>
        /// <param name="query">The query string to execute</param>
        public static IQuery CreateQuery(BaseLog log, ISettings settings, string query)
        {
            // store settings into the static variables for access by the QueryFunctions class
            if (string.IsNullOrEmpty(DateTimeFormat))
            {
                DateTimeFormat = settings.DateTimeFormat;
            }

            // build a tree for the query
            var tree = CreateTree(query);
            
            // rewrite the tree for optimization
            var rewrite = new RewriteVisitor();
            tree.Accept(rewrite);
            tree = rewrite.Tree;
           
            // create the actual query
            return new Query(log, tree);
        }

        /// <summary>
        /// Creates a tree from the given query
        /// </summary>
        /// <param name="query">Query to parse into a tree</param>
        /// <returns>The tree representation of the query</returns>
        public static Node CreateTree(string query)
        {
            var queryIdentifiers = new[]
            {
                "\"",
                "\\sand\\s",
                "\\sor\\s",
                "\\snot\\s",
                "\\sselect\\s",
                "\\swhere\\s",
            };

            Node tree;
            if (string.IsNullOrEmpty(query))
            {
                tree = new ScanNode();
            }
            else if (!queryIdentifiers.Any(i => Regex.IsMatch(query, i)) && !query.TrimStart().StartsWith("*"))
            {
                tree = new PhraseNode(query, false, null, null);
            }
            else
            {
                lock (_fieldTypes)
                {
                    tree = QueryParser.Parse(query);
                }
            }

            return tree;
        }

        #endregion

        #region private methods
        
        /// <summary>
        /// Creates a query tree for the given query elements
        /// </summary>
        /// <param name="match"><see cref="MatchNode"/> for matching log items</param>
        /// <param name="parser"><see cref="IParser"/> for parsing log items</param>
        /// <param name="filter">An <see cref="Expression"/> for filtering log items</param>
        /// <param name="grouping">An enumerable of tuples of group names and grouping <see cref="Expression"/>s for grouping the log items</param>
        /// <param name="selects">An enumerable of tuples of projection labels and <see cref="Expression"/>s for projecting log items</param>
        /// <param name="order">An enumerable of tuples indicating sorting fields and sort order</param>
        /// <param name="limit">The number of items to return</param>
        /// <returns>A query tree for executing the query</returns>
        private static Node CreateQueryTree(
            MatchNode match,
            IOption<Expression> filter,
            IOption<IEnumerable<Tuple<string, Expression>>> grouping,
            IOption<IEnumerable<Tuple<string, IAggregate>>> selects,
            IOption<IEnumerable<Tuple<string, bool>>> order,
            IOption<int> limit)
        {
            Node query = match;
            
            if (!filter.IsEmpty)
            {
                query = new PredicateNode(query, filter.Get());
            }

            if (!grouping.IsEmpty)
            {
                var keyNames = grouping.Get().Select(g => g.Item1).ToArray();
                var keyExpressions = grouping.Get().Select(g => g.Item2).ToArray();
                if (selects.IsEmpty)
                {
                    query = new GroupByNode(query, keyNames, keyExpressions, new string[0], new IAggregate[0]);
                }
                else
                {
                    var aggregateNames = selects.Get().Select(s => s.Item1).ToArray();
                    var aggregates = selects.Get().Select(s => s.Item2).ToArray();
                    query = new GroupByNode(query, keyNames, keyExpressions, aggregateNames, aggregates);
                }
            }
            else if (!selects.IsEmpty)
            {
                var aggregateNames = selects.Get().Select(s => s.Item1).ToArray();
                var aggregates = selects.Get().Select(s => s.Item2).ToArray();
                if (aggregates.All(a => a is ListAggregate))
                {
                    query = new ProjectNode(query, aggregateNames, aggregates.Cast<ListAggregate>().Select(a => a.Expression).ToArray());
                }
                else
                {
                    query = new AggregateNode(query, aggregateNames, aggregates);
                }
            }

            if (!order.IsEmpty)
            {
                query = new OrderByNode(query, order.Get().Select(a => a.Item1).ToArray(), order.Get().Select(a => a.Item2).ToArray());
            }

            if (!limit.IsEmpty)
            {
                query = new LimitNode(query, limit.Get());
            }

            return query;
        }

        /// <summary>
        /// Factory method for creating aggregate handlers
        /// </summary>
        /// <param name="name">The name of the aggregate function</param>
        /// <param name="input">The list of <see cref="Expression"/>s for calculating the aggregate</param>
        /// <returns>The aggregate implementation</returns>
        private static IAggregate CreateAggregate(string name, IEnumerable<Expression> input)
        {
            switch (name)
            {
                case "count": return new CountAggregate(input.ToArray());
                case "distinct": return new DistinctAggregate(input.ToArray());
                case "sum": return new SumAggregate(input.ToArray());
                case "mean": return new MeanAggregate(input.ToArray());
                case "median": return new MedianAggregate(input.ToArray());
                case "min": return new MinAggregate(input.ToArray());
                case "max": return new MaxAggregate(input.ToArray());
                case "first": return new FirstAggregate(input.ToArray());
                case "last": return new LastAggregate(input.ToArray());
                case "most": return new MostAggregate(input.ToArray());
                case "least": return new LeastAggregate(input.ToArray());
                default: return null;
            }
        }

        /// <summary>
        /// Creates a parser from the given name
        /// </summary>
        /// <param name="name">The name of the parser</param>
        /// <returns>An instance of the specified parser</returns>
        private static IParser CreateParser(string name)
        {
            switch (name)
            {
                case "json": return new JsonParser();
                case "csv": return new CsvParser();
                case "test": return new TestParser();
                default: throw new ParseException($"Undefined parser '{name}'");
            }
        }

        #endregion
    }
}
