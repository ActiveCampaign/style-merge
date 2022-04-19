using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using ExCSS;

namespace StyleMerge
{
    /// <summary>
    /// A selector, with specificity, and declarations.
    /// </summary>
    internal class RuleTuple
    {
        public int DocumentOrder { get; set; }
        public string Selector { get; set; }
        public Specificity Specificity { get; set; }
        public IEnumerable<IProperty> Properties { get; set; }
    }

    /// <summary>
    /// The primary entry point of this library. Use "ProcessHtml" for all your email inlining needs.
    /// </summary>
    public static class Inliner
    {
        private static readonly Regex PseudoClassSelector = new Regex(":(hover|link|visited|active|focus|target|first-letter|first-line|before|after|root)");
        private static readonly Regex DocTypeFinder = new Regex("^<!DOCTYPE [^>]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly StylesheetParser StylesheetParser = new StylesheetParser();
        public static readonly IHtmlParser HtmlParser;
        public static readonly ICssParser CssParser;

        static Inliner()
        {
            var context = BrowsingContext.New(Configuration.Default.WithCss());
            HtmlParser = context.GetService<IHtmlParser>();
            CssParser = context.GetService<ICssParser>();
        }

        /// <summary>
        /// Accepts a string of HTML and produces a string of HTML with styles inlined.
        /// </summary>
        /// <param name="sourceHtml"></param>
        public static string ProcessHtml(string sourceHtml)
        {
            var document = HtmlParser.ParseDocument(sourceHtml);

            foreach (var s in document.GetElementsByTagName("script"))
            {
                s.Remove();
            }

            var styleSheets = new List<(IElement Element, Stylesheet Styles)>();

            foreach (var element in document.GetElementsByTagName("style").ToArray())
            {
                try
                {
                    styleSheets.Add((element, StylesheetParser.Parse(element.InnerHtml)));
                }
                catch
                {
                    throw new InliningException("Not all stylesheet declarations in stylesheet beginning with '" + element.InnerHtml.Substring(0, 100) + "...' could be parsed.");
                }
            }

            var normalRules = new List<RuleTuple>();
            var importantRules = new List<RuleTuple>();

            //YIKES! this is hideous, but it's OK, it'll do what we need.
            var ruleIndex = 0;
            foreach (var styleSheet in styleSheets)
            {
                var uninlineable = new List<(string Selector, StyleDeclaration Style)>();

                var styleRules = styleSheet.Styles.StyleRules.Where(s => s.Selector != null).ToArray();
                foreach (var rule in styleRules)
                {
                    var selectors = rule.SelectorText.Split(',').ToLookup(k => true);
                    // MOST of the time, the selector isn't going to match.
                    // Therefore, we avoid regexing each selector in the string
                    // for only those cases where it does match.
                    if (PseudoClassSelector.IsMatch(rule.SelectorText))
                    {
                        selectors = rule.SelectorText.Split(',').ToLookup(k => !PseudoClassSelector.IsMatch(k));
                    }

                    var importantAndNot = rule.Style.ToLookup(k => k.IsImportant);

                    // Inline the safe rules per normal.
                    foreach (var selector in selectors[true])
                    {
                        ruleIndex++;
                        normalRules.Add(new RuleTuple
                        {
                            DocumentOrder = ruleIndex,
                            Properties = importantAndNot[false].ToArray(),
                            Selector = selector,
                            Specificity = selector.Specificity()
                        });
                        importantRules.Add(new RuleTuple
                        {
                            DocumentOrder = ruleIndex,
                            Properties = importantAndNot[true].ToArray(),
                            Selector = selector,
                            Specificity = new Specificity()
                        });
                    }

                    if (selectors[false].Any())
                    {
                        uninlineable.Add((string.Join(",", selectors[false]), rule.Style));
                    }
                }

                // Scrub all rules from the stylesheet
                var removeRules = styleSheet.Styles.StyleRules.ToArray();
                foreach (var rule in removeRules)
                {
                    styleSheet.Styles.RemoveChild(rule);
                }

                // If there are "left over" rules that could not be inlined (ie. pseudoselectors), we need 
                // to make sure those are applied back to the stylesheet so they do not just disappear
                foreach (var (Selector, Style) in uninlineable)
                {
                    var declarations = Style.Declarations.Select(x => $"{x.Name}: {x.Value} !important");
                    var rule = styleSheet.Styles.Add(RuleType.Style);
                    rule.Text = $"{Selector} {{ {string.Join(";", declarations)} }}";
                }

                // Apply the stylesheet content back to the element
                if (styleSheet.Styles.CharacterSetRules.Any() ||
                    styleSheet.Styles.FontfaceSetRules.Any() ||
                    styleSheet.Styles.ImportRules.Any() ||
                    styleSheet.Styles.MediaRules.Any() ||
                    styleSheet.Styles.NamespaceRules.Any() ||
                    styleSheet.Styles.PageRules.Any() ||
                    styleSheet.Styles.StyleRules.Any())
                {
                    styleSheet.Element.TextContent = styleSheet.Styles.ToCss();
                }
                else
                {
                    styleSheet.Element.Remove();
                }
            }

            var noApplyElements = new HashSet<IElement>(
                document.GetElementsByTagName("head")
                    .SelectMany(k => GetChildrenAndSelf(k)));

            ApplyRulesToElements(document, normalRules, noApplyElements);
            ApplyRulesToElements(document, importantRules, noApplyElements);

            //fix to original doctype
            var processed = document.ToHtml();
            var m = DocTypeFinder.Match(sourceHtml);
            if (m.Success)
            {
                processed = DocTypeFinder.Replace(processed, m.Value);
            }

            return processed;
        }

        private static IEnumerable<IElement> GetChildrenAndSelf(IElement element)
        {
            return element.Children.SelectMany(GetChildrenAndSelf).Concat(new IElement[] { element });
        }

        private static void ApplyRulesToElements(IHtmlDocument document, IEnumerable<RuleTuple> rules, HashSet<IElement> noApply)
        {
            var sortedRules = rules.OrderBy(k => k.Specificity).ThenBy(k => k.DocumentOrder);

            foreach (var rule in sortedRules)
            {
                try
                {
                    // All elements except for those determined to not get styles.
                    foreach (var node in document.QuerySelectorAll(rule.Selector).Where(k => !noApply.Contains(k)))
                    {
                        var styles = CssParser.ParseDeclaration(node.GetAttribute("style") ?? string.Empty);
                        
                        foreach (var prop in rule.Properties)
                        {
                            styles.SetProperty(prop.Name, prop.Value, prop.IsImportant ? "important" : null);
                        }

                        node.SetAttribute("style", styles.ToCss());
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