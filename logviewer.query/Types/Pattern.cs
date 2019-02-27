using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Types
{
    internal class Pattern
    {
        private readonly string _pattern;

        private readonly Token[] _tokens;

        private readonly string[] _captures;

        public string[] Captures => _captures;

        public Pattern(string pattern)
        {
            _pattern = pattern;
            _tokens = GenerateTokens(pattern);
            _captures = _tokens.Where(t => t.Type == 3).Select(t => t.Name).ToArray();
        }

        public override string ToString()
        {
            return _pattern;
        }

        public bool IsMatch(string s)
        {
            return Match(s).Success;
        }

        public override int GetHashCode()
        {
            return _tokens.Aggregate(0, (h, t) => h ^ t.Capture.GetHashCode() ^ t.Name.GetHashCode() ^ t.Type.GetHashCode() ^ t.Value.GetHashCode());
        }

        public Match Match(string data)
        {
            var captures = new Capture[Captures.Length];

            if (_tokens.Length == 1 && _tokens[0].Type == 1)
            {
                var result = data.IndexOf(_tokens[0].Value, 0, StringComparison.Ordinal);
                if (result >= 0)
                {
                    return new Match(result, _tokens[0].Value.Length);
                }
                else
                {
                    return new Match();
                }
            }
            else
            {
                var start = -1;
                var input = 0;
                var result = 0;
                for (var i = 0; i < _tokens.Length; i++)
                {
                    switch (_tokens[i].Type)
                    {
                        // literal match
                        case 0:
                            result = data.IndexOf(_tokens[i].Value, input, StringComparison.Ordinal);
                            if (result != input)
                            {
                                return new Match();
                            }
                            else
                            {
                                if (start < 0) start = input;
                                input += _tokens[i].Value.Length;
                            }
                            break;

                        // wildcard-* match
                        case 1:
                            if (_tokens[i].Value.Length == 0)
                            {
                                if (start < 0) start = input;
                                input = data.Length;
                            }
                            else
                            {
                                result = data.IndexOf(_tokens[i].Value, input, StringComparison.Ordinal);
                                if (result < 0)
                                {
                                    return new Match();
                                }
                                else
                                {
                                    if (start < 0) start = result;
                                    input = result + _tokens[i].Value.Length;
                                }
                            }
                            break;

                        // wildcard-? match
                        case 2:
                            if (start < 0) start = input;
                            input += 1;
                            break;

                        // capture match
                        case 3:
                            if (_tokens[i].Value.Length == 0)
                            {
                                captures[_tokens[i].Capture] = new Capture(input, data.Length - input, data.Substring(input));
                                if (start < 0) start = input;
                                input = data.Length;
                            }
                            else
                            {
                                result = data.IndexOf(_tokens[i].Value, input, StringComparison.Ordinal);
                                if (result < 0)
                                {
                                    return new Match();
                                }
                                else
                                {
                                    captures[_tokens[i].Capture] = new Capture(input, result - input, data.Substring(input, result - input));
                                    if (start < 0) start = input;
                                    input = result + _tokens[i].Value.Length;
                                }
                            }
                            break;

                    }
                }

                return new Match(start, input - start, captures);
            }
        }

        private Token[] GenerateTokens(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return new Token[0];
            }

            var tokens = new List<Token>();
            var i = 0;
            while (i < pattern.Length)
            {
                if (pattern[i] == '*')
                {
                    i += 1;
                    var start = i;
                    var follow = ReadLiteral(pattern, ref i);
                    if (start == i && i < pattern.Length - 1)
                    {
                        throw new InvalidOperationException("A wildcard must not be followed by another wildcard or capture");
                    }
                    else if (!string.IsNullOrEmpty(follow))
                    {
                        tokens.Add(new Token(1, follow, string.Empty, -1));
                    }
                }
                else if (pattern[i] == '?')
                {
                    tokens.Add(new Token(2, string.Empty, string.Empty, -1));
                    i += 1;
                }
                else if (pattern[i] == '{')
                {
                    var name = ReadCapture(pattern, ref i);
                    var start = i;
                    var follow = ReadLiteral(pattern, ref i);
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new InvalidOperationException("A capture must be named");
                    }
                    else if (start == i && i < pattern.Length - 1)
                    {
                        throw new InvalidOperationException("A wildcard must not be followed by another wildcard or capture");
                    }
                    else
                    {
                        tokens.Add(new Token(3, follow, name, tokens.Where(t => t.Type == 3).Count()));
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        tokens.Add(new Token(1, ReadLiteral(pattern, ref i), string.Empty, -1));
                    }
                    else
                    {
                        tokens.Add(new Token(0, ReadLiteral(pattern, ref i), string.Empty, -1));
                    }
                }
            }

            return tokens.ToArray();
        }

        private string ReadCapture(string pattern, ref int i)
        {
            var name = new StringBuilder();
            i += 1;
            for (; i < pattern.Length; i++)
            {
                if (pattern[i] == '}')
                {
                    i += 1;
                    return name.ToString();
                }
                else
                {
                    name.Append(pattern[i]);
                }
            }

            return null;
        }

        private string ReadLiteral(string pattern, ref int i)
        {
            var state = 0;
            var temp = new StringBuilder();
            for (; i < pattern.Length; i++)
            {
                switch (state)
                {
                    // regular input
                    case 0:
                        if (pattern[i] == '\\')
                        {
                            state = 1;
                        }
                        else if (pattern[i] == '*' || pattern[i] == '{' || pattern[i] == '?')
                        {
                            return temp.ToString();
                        }
                        else
                        {
                            temp.Append(pattern[i]);
                        }
                        break;

                    // escape sequence
                    case 1:
                        if (pattern[i] == '{' || pattern[i] == '*' || pattern[i] == '\\' || pattern[i] == '?')
                        {
                            temp.Append(pattern[i]);
                            state = 0;
                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown escape sequence");
                        }
                        break;
                }
            }

            return temp.ToString();
        }

        private struct Token
        {
            public Token(int type, string value, string name, int capture) { Type = type; Value = value; Name = name; Capture = capture; }
            public readonly int Type;
            public readonly string Value;
            public readonly string Name;
            public readonly int Capture;
        }
    }

    public class Capture
    {
        internal Capture(int index, int length, string value)
        {
            Index = index;
            Length = length;
            Value = value;
        }

        public readonly int Index;
        public readonly int Length;
        public readonly string Value;
    }

    public class Match
    {
        internal Match()
        {
            Success = false;
            Captures = new Capture[0];
        }

        internal Match(int index, int length, params Capture[] captures)
        {
            Success = true;
            Index = index;
            Length = length;
            Captures = captures;
        }

        public readonly bool Success;
        public readonly int Index;
        public readonly int Length;
        public readonly Capture[] Captures;
    }
}
