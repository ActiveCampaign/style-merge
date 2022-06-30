using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Text;

namespace StyleMerge
{
    public class CssFormatter : IStyleFormatter
    {
        private const string RgbaPattern = @"rgba[(](\d{1,3})\s?,\s?(\d{1,3})\s?,\s?(\d{1,3}),\s?(.*)[)]";
        private static readonly MatchEvaluator RgbaEvaluator = new MatchEvaluator(ReplaceRgba);

        public string Declaration(string name, string value, bool important)
        {
            var css = $"{name}: {value}{(important ? " !important" : string.Empty)}";

            // By default, AngleSharp uses RGBA to represent color values that have opacity, but for
            // older client compatibility, we want to use 'transparent' for completely transparent colors.
            css = Regex.Replace(css, RgbaPattern, RgbaEvaluator, RegexOptions.IgnoreCase);

            return css;
        }

        public string BlockDeclarations(IEnumerable<IStyleFormattable> declarations)
        {
            var sb = StringBuilderPool.Obtain()
                .Append(Symbols.CurlyBracketOpen);

            using (var writer = new StringWriter(sb))
            {
                foreach (var declaration in declarations)
                {
                    writer.Write(Symbols.Space);
                    declaration.ToCss(writer, this);
                    writer.Write(Symbols.Semicolon);
                }

                if (sb.Length > 1)
                {
                    sb.Remove(sb.Length - 1, 1);
                }
            }

            return sb.Append(Symbols.Space)
                .Append(Symbols.CurlyBracketClose)
                .ToPool();
        }

        public string BlockRules(IEnumerable<IStyleFormattable> rules)
        {
            var sb = StringBuilderPool.Obtain()
                .Append(Symbols.CurlyBracketOpen);

            using (var writer = new StringWriter(sb))
            {
                foreach (var rule in rules)
                {
                    writer.Write(Symbols.Space);
                    rule.ToCss(writer, this);
                }
            }

            return sb.Append(Symbols.Space)
                .Append(Symbols.CurlyBracketClose)
                .ToPool();
        }

        public string Rule(string name, string value)
        {
            return string.Concat(name, " ", value, ";");
        }

        public string Rule(string name, string prelude, string rules)
        {
            return string.Concat(name, " ", string.IsNullOrEmpty(prelude) ? string.Empty : prelude + " ", rules);
        }

        public string Comment(string data)
        {
            return $"/*{data}*/";
        }

        public string Sheet(IEnumerable<IStyleFormattable> rules)
        {
            var sb = StringBuilderPool.Obtain();
            var sep = Environment.NewLine;

            using (var writer = new StringWriter(sb))
            {
                foreach (var rule in rules)
                {
                    rule.ToCss(writer, this);
                    writer.Write(sep);
                }

                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - sep.Length, sep.Length);
                }
            }

            return sb.ToPool();
        }

        private static string ReplaceRgba(Match match)
        {
            // If alpha channel is completely empty we return 'transparent' for client compatibility. In most cases,
            // this was how the rule was specified anyway, AngleSharp is just converting it to RGBA representation.

            if (float.Parse(match.Groups[4].Value) == 0)
                return "transparent";

            return match.Value;
        }
    }
}
