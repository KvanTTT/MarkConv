using Xunit;

namespace MarkConv.Tests
{
    public class HtmlElementTests
    {
        [Fact]
        public void ShouldConvertDetailsToSpoilers()
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.GitHub,
                OutputMarkdownType = MarkdownType.Habr
            };

            Utils.CompareFiles("DetailsSummary.md", "DetailsSummary-to-Spoilers.md", options);
        }

        [Fact]
        public void ShouldConvertSpoilersToDetails()
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.Habr,
                OutputMarkdownType = MarkdownType.GitHub
            };

            Utils.CompareFiles("Spoilers.md", "Spoilers-to-DetailsSummary.md", options);
        }

        [Fact]
        public void ShouldConvertDetailsToDevFormat()
        {
            var options = new ProcessorOptions
            {
                InputMarkdownType = MarkdownType.GitHub,
                OutputMarkdownType = MarkdownType.Dev
            };

            Utils.CompareFiles("DetailsSummary.md", "DetailsSummary-to-DEV.md", options);
        }

        [Fact]
        public void ShouldRemoveSpoilers()
        {
            var options = new ProcessorOptions
            {
                InputMarkdownType = MarkdownType.Habr,
                RemoveSpoilers = true
            };

            Utils.CompareFiles("Spoilers.md", "Spoilers-Removed.md", options);
        }

        [Fact]
        public void ShouldRemoveComments()
        {
            var options = new ProcessorOptions
            {
                RemoveComments = true
            };

            Utils.CompareFiles("Comments.md", "Comments-Removed.md", options);
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
            var processor = new Processor(options);

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
            var processor = new Processor(options) {Logger = logger};

            string inputText = new string('a', LinksHtmlProcessor.HabrMaxTextLengthWithoutCut);
            processor.Process(inputText);
            Assert.Equal(LinksHtmlProcessor.HabrMaxTextLengthWithoutCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', LinksHtmlProcessor.HabrMaxTextLengthWithoutCut - 1);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', LinksHtmlProcessor.HabrMaxTextLengthBeforeCut + 1) + "<cut/>" +
                        new string('a', LinksHtmlProcessor.HabrMinTextLengthAfterCut);
            processor.Process(inputText);
            Assert.Equal(LinksHtmlProcessor.HabrMaxTextLengthBeforeCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', LinksHtmlProcessor.HabrMaxTextLengthBeforeCut) + "<cut/>" +
                        new string('a', LinksHtmlProcessor.HabrMinTextLengthAfterCut);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', LinksHtmlProcessor.HabrMinTextLengthBeforeCut - 1) + "<cut/>" +
                        new string('a', 1000);
            processor.Process(inputText);
            Assert.Equal(LinksHtmlProcessor.HabrMinTextLengthBeforeCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', LinksHtmlProcessor.HabrMinTextLengthBeforeCut) + "<cut/>" +
                        new string('a', 1000);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();

            inputText = new string('a', 1000) + "<cut/>" +
                        new string('a', LinksHtmlProcessor.HabrMinTextLengthAfterCut - 3);
            processor.Process(inputText);
            Assert.Equal(LinksHtmlProcessor.HabrMinTextLengthAfterCutMessage, logger.WarningMessages[0]);
            logger.Clear();

            inputText = new string('a', 1000) + "<cut/>" +
                        new string('a', LinksHtmlProcessor.HabrMinTextLengthAfterCut - 2);
            processor.Process(inputText);
            Assert.Empty(logger.WarningMessages);
            logger.Clear();
        }
    }
}
