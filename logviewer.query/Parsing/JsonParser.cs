using logviewer.query.Interfaces;
using Sprache;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Parsing
{
    internal class JsonParser : IParser
    {
        private static readonly Parser<string> String = (Parse.Char('\\').Then(c => Parse.AnyChar)).Or(Parse.AnyChar.Except(Parse.Char('"'))).Many().Text().Contained(Parse.Char('"'), Parse.Char('"'));

        private static readonly Parser<object> Value =
            String
            .Or(Parse.DecimalInvariant.Select(n => (object)double.Parse(n, CultureInfo.InvariantCulture)))
            .Or(Parse.Ref<object>(() => Object))
            .Or(Parse.Ref(() => Array))
            .Or(Parse.String("true").Select(_ => (object)true))
            .Or(Parse.String("false").Select(_ => (object)false))
            .Or(Parse.String("null").Select(_ => (object)null));

        private static readonly Parser<IEnumerable<KeyValuePair<string, object>>> KeyValuePair =
            from key in String
            from separator in Parse.Char(':').Token()
            from value in Value
            select Flatten(key, value);

        private static readonly Parser<IEnumerable<KeyValuePair<string, object>>> Object =
            from lparen in Parse.Char('{').Token()
            from values in KeyValuePair.DelimitedBy(Parse.Char(',').Token())
            from rparen in Parse.Char('}').Token()
            select values.SelectMany(v => v);

        private static readonly Parser<IEnumerable<KeyValuePair<string, object>>> Array =
            from lparen in Parse.Char('[').Token()
            from values in Value.DelimitedBy(Parse.Char(',').Token())
            from rparen in Parse.Char(']').Token()
            select values.SelectMany((v, i) => Flatten($"{i}", v));

        private static readonly Parser<IEnumerable<KeyValuePair<string, object>>> Json = Object.Or(Array).End();

        public bool TryParse(string input, out Dictionary<string, object> fields)
        {
            var result = Json.TryParse(input);

            if (result.WasSuccessful)
            {
                fields = result.Value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            else
            {
                fields = null;
            }

            return result.WasSuccessful;
        }

        private static IEnumerable<KeyValuePair<string, object>> Flatten(string key, object value)
        {
            if (value is IEnumerable<KeyValuePair<string, object>> list)
            {
                foreach (var v in list)
                {
                    yield return new KeyValuePair<string, object>($"{key}.{v.Key}", v.Value);
                }
            }
            else
            {
                yield return new KeyValuePair<string, object>(key, value);
            }
        }
    }
}
