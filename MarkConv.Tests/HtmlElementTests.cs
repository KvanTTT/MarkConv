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
                InputMarkdownType = MarkdownType.Common,
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
                OutputMarkdownType = MarkdownType.Common
            };

            Utils.CompareFiles("Spoilers.md", "Spoilers-to-DetailsSummary.md", options);
        }

        [Fact]
        public void ShouldRemoveSpoilers()
        {
            var options = new ProcessorOptions
            {
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
                OutputMarkdownType = MarkdownType.Common
            };
            var processor = new Processor(options);

            string source =
                "<anchor>key</anchor>\n" +
                "\n" +
                "## Header";
            string actual = processor.Process(source);
            Assert.Equal("## Header", actual);
        }
    }
}
