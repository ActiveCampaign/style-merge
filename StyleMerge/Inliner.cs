using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

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
        public IEnumerable<ICssProperty> Properties { get; set; }
    }

    /// <summary>
    /// The primary entry point of this library. Use "ProcessHtml" for all your email inlining needs.
    /// </summary>
    public static class Inliner
    {
        private static readonly Regex PseudoClassSelector = new Regex(":(hover|link|visited|active|focus|target|first-letter|first-line|before|after|root)");
        private static readonly Regex DocTypeFinder = new Regex("^<!DOCTYPE [^>]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly CssFormatter CssFormatter = new CssFormatter();
        private static readonly HtmlParser HtmlParser = new HtmlParser();
        private static readonly CssParser CssParser = new CssParser(new CssParserOptions
        {
            IsIncludingUnknownDeclarations = true,
            IsIncludingUnknownRules = true,
            IsToleratingInvalidSelectors = true
        });

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

            var styleSheets = new List<(IElement Element, ICssStyleSheet Styles)>();

            foreach (var element in document.GetElementsByTagName("style").ToArray())
            {
                try
                {
                    styleSheets.Add((element, CssParser.ParseStyleSheet(element.InnerHtml)));
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
            foreach (var (Element, Styles) in styleSheets)
            {
                var uninlineable = new List<(string Selector, ICssStyleDeclaration Style)>();

                var styleRules = Styles.Rules.OfType<ICssStyleRule>().ToArray();
                foreach (var rule in styleRules)
                {
                    var selectors = rule.SelectorText.Split(',').ToLookup(k => true);

                    // MOST of the time, the selector isn't going to match. We avoid regexing 
                    // every selector in the string for only those cases where it does match.
                    if (PseudoClassSelector.IsMatch(rule.SelectorText))
                    {
                        selectors = rule.SelectorText.Split(',')
                            .ToLookup(k => !PseudoClassSelector.IsMatch(k));
                    }

                    var importantAndNot = rule.Style.ToLookup(k => k.IsImportant);

                    // Inline the safe rules (ie. not pseudoselectors)
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

                // Scrub all 'basic' style rules (ie. ICssStyleRule) from the stylesheet. We must
                // preserve media, fontface, import, etc. rules, since they cannot be inlined.
                for (var i = Styles.Rules.Length - 1; i >= 0; i--)
                {
                    if (Styles.Rules.ElementAt(i) is ICssStyleRule)
                    {
                        Styles.RemoveAt(i);
                    }
                }

                // If there are rules that could not be inlined (ie. pseudoselectors), we need 
                // to make sure those are applied back to the stylesheet
                foreach (var (Selector, Style) in uninlineable)
                {
                    var declarations = Style.Select(x => $"{x.Name}: {x.Value} !important");
                    var rule = $"{Selector} {{ {string.Join(";", declarations)} }}";
                    Styles.Insert(rule, Styles.Rules.Length);
                }

                if (Styles.Rules.Any())
                {
                    Element.TextContent = Styles.ToCss(CssFormatter);
                }
                else
                {
                    Element.Remove();
                }
            }

            var noApplyElements = new HashSet<IElement>(
                document.GetElementsByTagName("head")
                    .SelectMany(k => GetChildrenAndSelf(k)));

            ApplyRulesToElements(document, normalRules, noApplyElements);
            ApplyRulesToElements(document, importantRules, noApplyElements);

            // Fix to original doctype
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
            var sortedRules = rules
                .OrderBy(k => k.Specificity)
                .ThenBy(k => k.DocumentOrder);

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
                            // TODO: We should probably lookup the existing property and only set it if it doesn't
                            // already exist. As things stand currently we are overwriting existing inline styles.
                            styles.SetProperty(prop.Name, prop.Value, prop.IsImportant ? "important" : null);
                        }

                        var styleAttribute = styles.ToCss(CssFormatter);
                        if (!string.IsNullOrWhiteSpace(styleAttribute))
                        {
                            node.SetAttribute("style", styleAttribute);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to apply rule to document - {ex.Message} (selector: {rule.Selector})");
                }
            }
        }
    }
}