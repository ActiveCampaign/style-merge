using CssInliner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace CssInliner.Tests
{
    internal static class WhitespaceNormalizer
    {
        private static Regex WHITESPACE_NORMALIZER = new Regex("[\\s]+", RegexOptions.Compiled);
        /// <summary>
        /// Provides a mechanism to make comparing expected and actual results a little more sane to author.
        /// You may include whitespace in resources to make them easier to read.
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        internal static string EliminateWhitespace(this string subject)
        {
            return WHITESPACE_NORMALIZER.Replace(subject, "");
        }
    }

    public class InlinerTestFixture
    {
        [Fact(Skip = @"The parser is pretty 'smart' about html, and will eat up just 
        about anything, even if it's not 'valid', therefore, this test doesn't prove anything.")]
        public void InlinerShouldParseRawHtml()
        {
            //This not throwing implicitly means that the parsing is OK.
            var result = Inliner.ProcessHtml(Inputs.HTML5_Boilerplate);
        }

        [Fact(Skip = @"The HTML parser we're using is 'smart' about malformed HTML, 
        and it's not clear we should be validating what the user provides 
        (some email clients may *require* malformed HTML to render properly).")]
        public void InlinerShouldRejectInvalidHtml()
        {
            Assert.Throws(typeof(Exception), () =>
            {
                var html = Inliner.ProcessHtml(Inputs.Malformed_HTML_1);
            });
            Assert.Throws(typeof(Exception), () =>
            {
                var html = Inliner.ProcessHtml(Inputs.Malformed_HTML_2);
            });
        }

        [Fact]
        public void InlinerShouldEliminateScriptBlocks()
        {
            var processed = Inliner.ProcessHtml(Inputs.InlinerShouldEliminateScriptBlocks).EliminateWhitespace();
            Assert.Equal(Outputs.Expected_InlinerShouldEliminateScriptBlocks.EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerShouldKeepMediaQueryStylesInStyleBlocks()
        {
            var processed = Inliner.ProcessHtml(Inputs.InlinerShouldKeepMediaQueryStylesInStyleBlocks).EliminateWhitespace();
            Assert.Equal(Outputs.Expected_InlinerShouldKeepMediaQueryStylesInStyleBlocks.EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerShouldApplyStylesInDocumentOrder()
        {
            var processed = Inliner.ProcessHtml(Inputs.InlinerShouldApplyStylesInDocumentOrder).EliminateWhitespace();
            Assert.Equal(Outputs.Expected_InlinerShouldApplyStylesInDocumentOrder.EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerShouldApplyStylesAccordingToSpecificityValues()
        {
            var processed = Inliner.ProcessHtml(Inputs.InlinerShouldApplyStylesAccordingToSpecificityValues).EliminateWhitespace();
            Assert.Equal(Outputs.Expected_InlinerShouldApplyStylesAccordingToSpecificityValues.EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerShouldApplyStylesForRulesWithMultipleSelectors()
        {
            var processed = Inliner.ProcessHtml(Inputs.InlinerShouldApplyStylesForRulesWithMultipleSelectors).EliminateWhitespace();
            Assert.Equal(Outputs.Expected_InlinerShouldApplyStylesForRulesWithMultipleSelectors.EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerShouldEliminateStyleBlocksWhereAllRulesAreInlined()
        {
            var processed = Inliner.ProcessHtml(Inputs.InlinerShouldEliminateStyleBlocksWhereAllRulesAreInlined).EliminateWhitespace();
            Assert.Equal(Outputs.Expected_InlinerShouldEliminateStyleBlocksWhereAllRulesAreInlined.EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerShouldFixDoctypeToOriginalAfterProcessing()
        {
            var processed = Inliner.ProcessHtml(Inputs.html_with_double_quote_doctype).EliminateWhitespace();
            Assert.Equal((Inputs.html_with_double_quote_doctype).EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerShouldNotApplyStylesToHead()
        {
            var processed = Inliner.ProcessHtml(Inputs.universal_selector_shouldnt_apply_styles_to_head_and_children).EliminateWhitespace();
            Assert.Equal(Outputs.universal_selector_doesnt_apply_to_head.EliminateWhitespace(), processed);
        }


        [Fact]
        public void InlinerShouldProperlyHandleDoubleQuotesInDeclarations()
        {
            var processed = Inliner.ProcessHtml(Inputs.inlined_url_declarations_should_not_have_spaces_added).EliminateWhitespace();
            Assert.Equal(Outputs.style_sheet_attributes_with_quotes_should_be_handled_properly.EliminateWhitespace(), processed);
        }

        [Fact]
        public void InlinerCanParseAndInlineEmailACIDTestCSS()
        {
            Assert.Throws<InliningException>(() => Inliner.ProcessHtml(Inputs.EmailACIDTest));
        }

    }
}
