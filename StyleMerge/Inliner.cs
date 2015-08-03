using CsQuery;
using CsQuery.Implementation;
using ExCSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StyleMerge
{
    /// <summary>
    /// A selector, with specificity, and declarations.
    /// </summary>
    internal class RuleTuple
    {
        public int DocumentOrder { get; set; }
        public Specificity Specificity { get; set; }
        public string Selector { get; set; }
        public string Declarations { get; set; }
    }

    /// <summary>
    /// The primary entry point of this library. Use "ProcessHtml" for all your email inlining needs.
    /// </summary>
    public static class Inliner
    {
        private static readonly Regex _pseudoclassSelector = new Regex(":(hover|link|visited|active|focus|target|first-letter|first-line|before|after)");

        /// <summary>
        /// Accepts a string of HTML and produces a string of HTML with styles inlined.
        /// </summary>
        /// <param name="sourceHtml"></param>
        /// <returns></returns>
        public static String ProcessHtml(string sourceHtml)
        {
            var document = CQ.CreateDocument(sourceHtml);

            //Console.WriteLine(document.Document.DocType);

            foreach (var s in document["script"].Elements)
            {
                s.Remove();
            }

            var sheets = document["style"].Elements.OfType<HTMLStyleElement>().ToArray();

            var parser = new ExCSS.Parser();
            var parsedStylesheets = new List<Tuple<HTMLStyleElement, StyleSheet>>();

            foreach (var k in sheets)
            {
                try
                {
                    parsedStylesheets.Add(Tuple.Create(k, parser.Parse(k.InnerHTML)));
                }
                catch
                {
                    throw new InliningException("Not all stylesheet declarations in stylesheet beginning with '" + k.InnerHTML.Substring(0, 100) + "...' could be parsed.");
                }
            }

            var normalRules = new List<RuleTuple>();
            var importantRules = new List<RuleTuple>();


            //YIKES! this is hideous, but it's OK, it'll do what we need.
            var ruleIndex = 0;
            foreach (var styleSheet in parsedStylesheets)
            {
                var uninlineable = new List<Tuple<string, StyleDeclaration>>();

                var styleRules = styleSheet.Item2.StyleRules.ToArray();
                foreach (var s in styleRules)
                {
                    var selectors = s.Selector.ToString().Split(',').ToLookup(k => true);
                    // MOST of the time, the selector isn't going to match.
                    // Therefore, we avoid regexing each selector in the string
                    // for only those cases where it does match.
                    if (_pseudoclassSelector.IsMatch(s.Selector.ToString()))
                    {
                        selectors = s.Selector.ToString().Split(',').ToLookup(k => !_pseudoclassSelector.IsMatch(k));
                    }

                    var importantAndNot = s.Declarations.ToLookup(k => k.Important);

                    //inline the safe rules per normal.
                    foreach (var selector in selectors[true])
                    {
                        ruleIndex++;
                        normalRules.Add(new RuleTuple()
                        {
                            DocumentOrder = ruleIndex,
                            Declarations = importantAndNot[false]
                                .Aggregate("", (seed, current) => seed += String.Format("{0}:{1};", current.Name, current.Term)),
                            Selector = selector,
                            Specificity = selector.Specificity()
                        });
                        importantRules.Add(new RuleTuple()
                        {
                            DocumentOrder = ruleIndex,
                            Declarations = importantAndNot[true]
                                .Aggregate("", (seed, current) => seed += String.Format("{0}:{1} !important;", current.Name, current.Term)),
                            Selector = selector,
                            Specificity = new Specificity()
                        });
                    }

                    if (selectors[false].Any())
                    {
                        uninlineable.Add(Tuple.Create(String.Join(",", selectors[false]), s.Declarations));
                    }
                }

                //scrub all rules from the stylesheet.
                styleSheet.Item2.Rules.RemoveAll(k => k is StyleRule);

                //if there are "left over" rules, we want to make sure those are applied.
                foreach (var i in uninlineable)
                {
                    foreach (var d in i.Item2)
                    {
                        d.Important = true;
                    }

                    styleSheet.Item2.Rules.Add(new StyleRule(i.Item2)
                    {
                        Value = i.Item1
                    });
                }

                //apply the stylesheet content back to the CsQuery object.
                if (styleSheet.Item2.Rules.Any())
                {
                    styleSheet.Item1.InnerText = styleSheet.Item2.ToString();
                }
                else
                {
                    styleSheet.Item1.Remove();
                }
            }


            var noApplyElements = new HashSet<IDomElement>(document.Select("head").Elements.SelectMany(k => GetAllChildren(k)));

            ApplyRulesToElements(document, normalRules, noApplyElements);
            ApplyRulesToElements(document, importantRules, noApplyElements);

            //fix to original doctype
            var processed = document.Render();
            var m = _doctypeFinder.Match(sourceHtml);
            if (m.Success)
            {
                processed = _doctypeFinder.Replace(processed, m.Value);
            }

            return processed;
        }

        private static IEnumerable<IDomElement> GetAllChildren(IDomElement element)
        {
            return element.ChildElements.SelectMany(GetAllChildren).Concat(new IDomElement[] { element });
        }

        private static Regex _doctypeFinder = new Regex("^<!DOCTYPE [^>]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static void ApplyRulesToElements(CQ document, IEnumerable<RuleTuple> rules, HashSet<IDomElement> noApply)
        {
            var sortedRules = rules.OrderBy(k => k.Specificity).ThenBy(k => k.DocumentOrder);

            foreach (var rule in sortedRules)
            {
                try
                {
                    //all the elements except for those determined to not get styles.
                    foreach (var a in document[rule.Selector].Elements.Where(k => !noApply.Contains(k)))
                    {
                        a.Style.AddStyles(rule.Declarations, false);
                    }
                }
                catch (NotImplementedException ex)
                {
                    if (ex.Message == "Pseudoclasses that require a browser aren't implemented.")
                    {
                        throw new InliningException("Pseudoclasses that require a browser, such as '" + rule.Selector + "', are not supported for inlining.");
                    }
                }
            }
        }
    }
}