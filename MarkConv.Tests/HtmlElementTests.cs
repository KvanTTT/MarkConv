using Xunit;

namespace MarkConv.Tests
{
    public class HtmlElementTests : TestsBase
    {
        [Fact]
        public void ShouldReportHtmlParseErrors()
        {
            var logger = new Logger();
            var processor = new Processor(new ProcessorOptions(), logger);
            processor.Process(ReadFileFromResources("HtmlParseErrors.md"));

            var messages = logger.WarningMessages;
            Assert.Equal(6, messages.Count);
            Assert.Equal("Incorrect nesting: element </x> at [3,3..4) closes <y> at [2,2..3)", messages[0]);
            Assert.Equal("Incorrect nesting: element </y> at [4,3..4) closes <x> at [1,2..3)", messages[1]);
            Assert.Equal("Parse error: extraneous input '/' expecting {'area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr', 'cut', TAG_NAME} at [9,2..3)", messages[2]);
            Assert.Equal("Parse error: mismatched input 'EOF' expecting {MARKDOWN_FRAGMENT, '<', HTML_COMMENT, HTML_TEXT} at [9,5)", messages[3]);
            Assert.Equal("Element <img> does not contain required 'src' attribute at [6,2..5)", messages[4]);
            Assert.Equal("Element <a> does not contain required 'href' attribute at [7,2..3)", messages[5]);
        }

        [Theory]
        [InlineData(MarkdownType.GitHub, MarkdownType.Habr)]
        [InlineData(MarkdownType.GitHub, MarkdownType.Dev)]
        //[InlineData(MarkdownType.Habr, MarkdownType.GitHub)] //TODO
        //[InlineData(MarkdownType.Habr, MarkdownType.Dev)] //TODO
        public void ShouldConvertDetailsSummary(MarkdownType inMarkdownType, MarkdownType outMarkdownType)
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = inMarkdownType,
                OutputMarkdownType = outMarkdownType
            };

            CompareFiles($"DetailsSummary.{inMarkdownType}.md", $"DetailsSummary.{outMarkdownType}.md", options);
        }

        [Fact]
        public void ShouldRemoveDetails()
        {
            var options = new ProcessorOptions
            {
                InputMarkdownType = MarkdownType.GitHub,
                RemoveDetails = true
            };

            var processor = new Processor(options, new Logger());
            var actual = processor.Process(ReadFileFromResources("DetailsSummary.GitHub.md"));
            Assert.True(string.IsNullOrWhiteSpace(actual));
        }

        [Fact]
        public void ShouldRemoveComments()
        {
            var options = new ProcessorOptions
            {
                RemoveComments = true
            };

            CompareFiles("Comments.md", "Comments-Removed.md", options);
        }

        [Fact]
        public void ShouldIgnoreCaseOfHtmlNames()
        {
            var logger = new Logger();
            var processor = new Processor(new ProcessorOptions(), logger);
            var result = processor.Process(@"<details>
<SUMMARY>title </summary>
CONTENT
</DETAILS>");
            Assert.Equal(@"<details>
<summary>title </summary>
CONTENT 
</details>", result);
            Assert.Empty(logger.WarningMessages);
        }

        [Fact]
        public void ShouldCorrectlyFormatHtml()
        {
            var logger = new Logger();
            var processor = new Processor(new ProcessorOptions() { LinesMaxLength = -1 }, logger);
            var result = processor.Process(@"<details>
<b>bold</b>,<i>italic</i>,   <s>strike</s>
Next Line
</details>");

            Assert.Equal(@"<details>
<b>bold</b>,<i>italic</i>, <s>strike</s> Next Line 
</details>", result);
        }

        [Fact]
        public void ShouldWarnAboutCutRestrictions()
        {
            var options = new ProcessorOptions { OutputMarkdownType = MarkdownType.Habr };
            var logger = new Logger();
            var processor = new Processor(options, logger);

            string inputText = new string('a', Postprocessor.HabrMaxTextLengthWithoutCut);
            processor.Process(inputText);
            Assert.Equal(Postprocessor.HabrMaxTextLengthWithoutCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', Postprocessor.HabrMaxTextLengthWithoutCut - 1);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', Postprocessor.HabrMaxTextLengthBeforeCut + 1) + "<cut/>" +
                        new string('a', Postprocessor.HabrMinTextLengthAfterCut);
            processor.Process(inputText);
            Assert.Equal(Postprocessor.HabrMaxTextLengthBeforeCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', Postprocessor.HabrMaxTextLengthBeforeCut - 1) + "<cut/>" +
                        new string('a', Postprocessor.HabrMinTextLengthAfterCut);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', Postprocessor.HabrMinTextLengthBeforeCut - 2) + "<cut/>" +
                        new string('a', 1000);
            processor.Process(inputText);
            Assert.Equal(Postprocessor.HabrMinTextLengthBeforeCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', Postprocessor.HabrMinTextLengthBeforeCut) + "<cut/>" +
                        new string('a', 1000);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', 1000) + "<cut/>" +
                        new string('a', Postprocessor.HabrMinTextLengthAfterCut - 4);
            processor.Process(inputText);
            Assert.Equal(Postprocessor.HabrMinTextLengthAfterCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', 1000) + "<cut/>" +
                        new string('a', Postprocessor.HabrMinTextLengthAfterCut - 2);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();
        }
    }
}
