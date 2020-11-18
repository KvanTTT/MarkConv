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
            Assert.Equal(5, messages.Count);
            Assert.Equal("Incorrect nesting: element </x> at [3,3..4) closes <y> at [2,2..3)", messages[0]);
            Assert.Equal("Incorrect nesting: element </y> at [4,3..4) closes <x> at [1,2..3)", messages[1]);
            Assert.Equal("Parse error: extraneous input '/' expecting {'area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr', 'cut', TAG_NAME} at [9,2..3)", messages[2]);
            Assert.Equal("Element <img> does not contain required 'src' attribute at [6,2..5)", messages[3]);
            Assert.Equal("Element <a> does not contain required 'href' attribute at [7,2..3)", messages[4]);
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
        public void ShouldConvertAnchors()
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.Habr,
                OutputMarkdownType = MarkdownType.GitHub
            };
            var processor = new Processor(options, new Logger());

            string source =
                "<anchor>key</anchor>\n" +
                "\n" +
                "## Header";
            string actual = processor.Process(source);
            Assert.Equal("## Header", actual);
        }

        [Fact]
        public void ShouldWarnAboutCutRestrictions()
        {
            var options = new ProcessorOptions { OutputMarkdownType = MarkdownType.Habr };
            var logger = new Logger();
            var processor = new Processor(options, logger);

            string inputText = new string('a', HabrConstsAndMessages.HabrMaxTextLengthWithoutCut);
            processor.Process(inputText);
            Assert.Equal(HabrConstsAndMessages.HabrMaxTextLengthWithoutCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', HabrConstsAndMessages.HabrMaxTextLengthWithoutCut - 1);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', HabrConstsAndMessages.HabrMaxTextLengthBeforeCut + 1) + "<cut/>" +
                        new string('a', HabrConstsAndMessages.HabrMinTextLengthAfterCut);
            processor.Process(inputText);
            Assert.Equal(HabrConstsAndMessages.HabrMaxTextLengthBeforeCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', HabrConstsAndMessages.HabrMaxTextLengthBeforeCut) + "<cut/>" +
                        new string('a', HabrConstsAndMessages.HabrMinTextLengthAfterCut);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', HabrConstsAndMessages.HabrMinTextLengthBeforeCut - 1) + "<cut/>" +
                        new string('a', 1000);
            processor.Process(inputText);
            Assert.Equal(HabrConstsAndMessages.HabrMinTextLengthBeforeCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', HabrConstsAndMessages.HabrMinTextLengthBeforeCut) + "<cut/>" +
                        new string('a', 1000);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', 1000) + "<cut/>" +
                        new string('a', HabrConstsAndMessages.HabrMinTextLengthAfterCut - 3);
            processor.Process(inputText);
            Assert.Equal(HabrConstsAndMessages.HabrMinTextLengthAfterCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', 1000) + "<cut/>" +
                        new string('a', HabrConstsAndMessages.HabrMinTextLengthAfterCut - 2);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();
        }
    }
}
