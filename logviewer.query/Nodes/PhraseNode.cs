using logviewer.Interfaces;
using logviewer.query.Interfaces;
using logviewer.query.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace logviewer.query.Nodes
{
    /// <summary>
    /// A node constructing a filter using a filter phrase
    /// </summary>
    internal class PhraseNode : MatchNode
    {
        private readonly string _dateTimeFormat;
        private readonly string _phrase;
        private readonly Pattern _pattern;
        private readonly Token[] _tokens;
        private readonly Dictionary<string, Type> _fields = new Dictionary<string, Type>();
        private readonly bool _exact;
        private readonly string _markerField;
        private readonly string _markerValue;

        public PhraseNode(string pattern, bool exact, string markerField, string markerValue)
        {
            _phrase = pattern;
            _fields["message"] = typeof(string);
            _dateTimeFormat = QueryFactory.DateTimeFormat;

            // generate a pattern for matching fields
            _pattern = new Pattern(pattern);
            for (var i = 0; i < _pattern.Captures.Length; i++)
            {
                var split = _pattern.Captures[i].IndexOf(':');
                var type = typeof(string);
                var name = _pattern.Captures[i];
                if (split >= 0)
                {
                    name = _pattern.Captures[i].Substring(0, split);
                    var parameter = _pattern.Captures[i].Substring(split + 1);
                    switch (parameter)
                    {
                        case "number": type = typeof(double); break;
                        case "string": type = typeof(string); break;
                        case "time": type = typeof(DateTime?); break;
                        default:
                            if (parameter.IndexOfAny(new[] { 'd', 'M', 'y', 'h', 'H', 'm', 's', 'f' }) >= 0)
                            {
                                try
                                {
                                    DateTime.Now.ToString(parameter);
                                    _dateTimeFormat = parameter;
                                    type = typeof(DateTime?);
                                }
                                catch
                                {
                                    throw new ArgumentException($"Undefined field type {parameter}");
                                }
                            }
                            else
                            {
                                throw new ArgumentException($"Undefined field type {parameter}");
                            }
                            break;
                    }
                }

                _pattern.Captures[i] = name;
                _fields[name] = type;
            }

            // store the marker field/value
            _markerField = markerField;
            _markerValue = markerValue;
            if (!string.IsNullOrEmpty(_markerField) && !string.IsNullOrEmpty(_markerValue))
            {
                _fields[_markerField] = typeof(string);
            }

            // generate tokens for inverted index based filtering
            var nextIsExact = true;
            var tokens = new List<Token>();
            long position = 0;
            foreach (var token in new TokenReader(pattern, string.Empty, string.Empty, 0, Encoding.Default).ReadAll().Where(t => t.Type == ETokenType.Characters && t.Data.Length >= 3))
            {
                if (IsTokenInCapture(pattern, token))
                {
                    nextIsExact = false;
                }
                else
                {
                    // if the token is preceded by a wildcard the position does not match exactly
                    if (IsTokenPrecededByWildcard(pattern, token, tokens.LastOrDefault()))
                    {
                        nextIsExact = false;
                        if (position != token.Position) exact = true;
                        position = token.Position + token.Data.Length;
                    }
                    else
                    {
                        position += token.Data.Length;
                    }

                    tokens.Add(new Token() { Type = ETokenType.Characters, Data = token.Data, IsExact = nextIsExact, Position = token.Position });
                    nextIsExact = true;
                }
            }
            _tokens = tokens.ToArray();
            if (position != pattern.Length) exact = true;

            // set the flag for exact matching
            _exact = exact || _tokens.Length == 0 || _fields.Count > 1 || (!string.IsNullOrEmpty(_markerField) && !string.IsNullOrEmpty(_markerValue));
        }

        public IDictionary<string, Type> Fields => _fields;

        public Token[] Tokens => _tokens;

        public string Phrase => _phrase;

        public bool Exact => _exact;

        public override void Accept(IVisitor visitor) => visitor.Visit(this);

        public override Expression Predicate() => Expression.Call(Expression.Constant(this), GetType().GetMethod(nameof(IsMatch), BindingFlags.Instance | BindingFlags.NonPublic), QueryFactory.ItemVariable);
        
        private bool IsMatch(LogItem item)
        {
            var match = _pattern.Match(item.Message);
            if (match.Success)
            {
                if (match.Captures.Length > 0)
                {
                    // store captured field values
                    for (var i = 0; i < _pattern.Captures.Length; i++)
                    {
                        var type = _fields[_pattern.Captures[i]];
                        if (type == typeof(DateTime?))
                        {
                            item.Fields[_pattern.Captures[i]] = QueryFunctions.time(match.Captures[i].Value, _dateTimeFormat);
                        }
                        else
                        {
                            try
                            {
                                item.Fields[_pattern.Captures[i]] = Convert.ChangeType(match.Captures[i].Value, type, CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }
                }

                // rewrite the message if requested
                if (!string.IsNullOrEmpty(_markerField) && !string.IsNullOrEmpty(_markerValue))
                {
                    item.Fields[_markerField] = _markerValue;
                }
            }

            return match.Success;
        }

        private bool IsTokenPrecededByWildcard(string pattern, Token token, Token previousToken)
        {
            var wildcard = pattern.LastIndexOf('*', (int)token.Position);
            return !string.IsNullOrEmpty(previousToken.Data) && wildcard >= 0 && wildcard >= previousToken.Position + previousToken.Data.Length;
        }

        private bool IsTokenPrecededByNonToken(string pattern, Token token, Token previousToken)
        {
            return !string.IsNullOrEmpty(previousToken.Data) && token.Position != previousToken.Position + previousToken.Data.Length;
        }

        private bool IsTokenInCapture(string pattern, Token token)
        {
            var openerBeforeToken = pattern.LastIndexOf('{', (int)token.Position);
            var closerBeforeToken = pattern.LastIndexOf('}', (int)token.Position);
            var openerAfterToken = pattern.IndexOf('{', (int)token.Position);
            var closerAfterToken = pattern.IndexOf('}', (int)token.Position);
            return openerBeforeToken >= 0 && closerBeforeToken < openerBeforeToken && closerAfterToken >= 0 && (openerAfterToken < 0 || openerAfterToken > closerAfterToken);
        }
    }
}
