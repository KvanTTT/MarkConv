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
                InputMarkdownType = MarkdownType.VisualCode,
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
                OutputMarkdownType = MarkdownType.VisualCode
            };

            Utils.CompareFiles("Spoilers.md", "Spoilers-to-DetailsSummary.md", options);
        }

        [Fact]
        public void ShouldConvertAnchors()
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.Habr,
                OutputMarkdownType = MarkdownType.VisualCode
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
