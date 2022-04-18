using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StyleMerge.Tests.TestCases;
using Xunit;

namespace StyleMerge.Tests
{
    internal static class WhitespaceNormalizer
    {
        private static readonly Regex WhitespacePattern = new Regex("[\\s]+", RegexOptions.Compiled);

        /// <summary>
        /// Provides a mechanism to make comparing expected and actual results a little more sane to author.
        /// You may include whitespace in resources to make them easier to read.
        /// </summary>
        internal static string EliminateWhitespace(this string subject)
        {
            return WhitespacePattern.Replace(subject, "");
        }
    }

    public class InlinerTests
    {
        private readonly TestCaseLoader _loader;

        public InlinerTests()
        {
            _loader = new TestCaseLoader();
        }

        [Theory]
        [InlineData("ShouldApplyStylesInDocumentOrder")]
        [InlineData("ShouldApplyStylesAccordingToSpecificityValues")]
        [InlineData("ShouldApplyStylesForRulesWithMultipleSelectors")]
        [InlineData("ShouldEliminateStyleBlocksWhereAllRulesAreInlined")]
        [InlineData("ShouldEliminateScriptBlocks")]
        [InlineData("ShouldKeepMediaQueryStylesInStyleBlocks")]
        [InlineData("ShouldFixDoctypeToOriginalAfterProcessing")]
        [InlineData("ShouldNotApplyStylesToHead")]
        [InlineData("ShouldProperlyHandleDoubleQuotesInDeclarations")]
        [InlineData("ShouldMaintainImportantDeclarations")]
        [InlineData("ShouldSkipInvalidCSSDeclarations")]
        public async Task ShouldHandleTestCase(string test)
        {
            await VerifyTestCase(test);
        }
        
        [Fact(Skip = "Not sure why this test exists, it doesn't really verify anything")]
        public async Task CanParseAndInlineEmailACIDTestCSS()
        {
            var data = await _loader.LoadData("CanParseAndInlineEmailACIDTestCSS");

            // TODO: this should attempt to produce exactly same result as Premailer.
            Inliner.ProcessHtml(data.Input);
        }

        [Theory]
        [InlineData(":link")]
        [InlineData(":hover")]
        [InlineData(":active")]
        [InlineData(":focus")]
        [InlineData(":visited")]
        [InlineData(":target")]
        [InlineData("::first-letter")]
        [InlineData("::first-line")]
        [InlineData("::before")]
        [InlineData("::after")]
        //[InlineData(":nth-child(n)")]
        //[InlineData(":nth-last-child(n)")]
        //[InlineData(":nth-of-type(n)")]
        //[InlineData(":nth-last-of-type(n)")]
        //[InlineData(":first-child")]
        //[InlineData(":last-child")]
        //[InlineData(":first-of-type")]
        //[InlineData(":last-of-type")]
        //[InlineData(":empty")]
        public async Task ShouldHandlePseudoSelectors(string psuedoSelector)
        {
            var data = await _loader.LoadData("ShouldHandlePseudoSelectors");
            var input = data.Input.Replace("~~TEST_SELECTOR~~", psuedoSelector);
            var output = data.Output.Replace("~~TEST_SELECTOR~~", psuedoSelector);

            var processed = Inliner.ProcessHtml(input);
            
            Assert.Equal(output.EliminateWhitespace(), processed.EliminateWhitespace());
        }

        private async Task VerifyTestCase(string key)
        {
            var data = await _loader.LoadData(key);
            var processed = Inliner.ProcessHtml(data.Input);

            Assert.Equal(
                data.Output.EliminateWhitespace(),
                processed.EliminateWhitespace());
        }
    }
}
