using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StyleMerge
{
    internal static class SpecificityCalculator
    {
        internal static Specificity Specificity(this string subject)
        {
            var retval = new Specificity();
            subject = _notScrubber.Replace(subject, "$1");
            foreach (var a in _rulers)
            {
                var number = a.Item2.Matches(subject).Count;
                retval[a.Item1] += number;
                subject = a.Item2.Replace(subject, "");
            }
            return retval;
        }

        private static Regex _notScrubber = new Regex(":not\\(([^\\)]*)\\)", RegexOptions.ECMAScript | RegexOptions.Compiled);

        // These Regexen are from here:
        // https://github.com/keeganstreet/specificity/blob/master/specificity.js
        private static List<Tuple<string, Regex>> _rulers = new List<Tuple<string, Regex>>
        {
            //Attributes
            Tuple.Create<string, Regex>("b", new Regex("(\\[[^\\]]+\\])", RegexOptions.ECMAScript| RegexOptions.Compiled)),
            //Ids
            Tuple.Create<string, Regex>("a", new Regex("(#[^\\s\\+>~\\.\\[:]+)", RegexOptions.ECMAScript| RegexOptions.Compiled)),
            //classes
            Tuple.Create<string, Regex>("b", new Regex("(\\.[^\\s\\+>~\\.\\[:]+)", RegexOptions.ECMAScript| RegexOptions.Compiled)),
            //Pseudo-elements
            Tuple.Create<string, Regex>("c", new Regex("(::[^\\s\\+>~\\.\\[:]+|:first-line|:first-letter|:before|:after)", RegexOptions.ECMAScript | RegexOptions.IgnoreCase| RegexOptions.Compiled)),
            //Pseudo-elements with brackets
            Tuple.Create<string, Regex>("b", new Regex("(:[\\w-]+\\([^\\)]*\\))", RegexOptions.ECMAScript | RegexOptions.IgnoreCase| RegexOptions.Compiled)),
            //scrub the * selector.
            Tuple.Create<string, Regex>(" ", new Regex("([\\*\\s\\+>~])", RegexOptions.ECMAScript | RegexOptions.Compiled)),
            //Psuedo-classes
            Tuple.Create<string, Regex>("b", new Regex("(:[^\\s\\+>~\\.\\[:]+)", RegexOptions.ECMAScript | RegexOptions.Compiled)),
            //Elements
            Tuple.Create<string, Regex>("c", new Regex("([^\\s\\+>~\\.\\[:]+)", RegexOptions.ECMAScript | RegexOptions.Compiled)),
        };
    }

    internal class Specificity : IComparable<Specificity>
    {
        private Dictionary<string, int> _values = new Dictionary<string, int>(4);

        public int this[string index]
        {
            get { if (!_values.ContainsKey(index)) { _values[index] = 0; } return _values[index]; }
            set { _values[index] = value; }
        }

        public int CompareTo(Specificity other)
        {
            var retval = this["a"].CompareTo(other["a"]);
            if (retval == 0)
            {
                retval = this["b"].CompareTo(other["b"]);
                if (retval == 0)
                {
                    retval = this["c"].CompareTo(other["c"]);
                }
            }
            return retval;
        }
    }

}
