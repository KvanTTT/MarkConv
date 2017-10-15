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
                OutputMarkdownType = MarkdownType.Habrahabr
            };
            var processor = new Processor(options);

            string source = Utils.ReadFileFromProject("DetailsSummary.md");
            string actual = processor.Process(source);
            Assert.Equal(
                "<spoiler title=\"Details\">\n" +
                "Content\n" +
                "\n" +
                "```\n" +
                "Some code\n" +
                "```\n" +
                "\n" +
                "<spoiler title=\"Nested Details\">\n" +
                "Nested text\n" +
                "</spoiler>\n" +
                "</spoiler>", actual);
        }

        [Fact]
        public void ShouldConvertSpoilersToDetails()
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.Habrahabr,
                OutputMarkdownType = MarkdownType.VisualCode
            };
            var processor = new Processor(options);

            string source = Utils.ReadFileFromProject("Spoilers.md");
            string actual = processor.Process(source);
            Assert.Equal(
                "<details>\n" +
                "<summary>Spoiler header</summary>\n" +
                "\n" +
                "Content\n" +
                "\n" +
                "```\n" +
                "Some code\n" +
                "```\n" +
                "\n" +
                "<details>\n" +
                "<summary>Nested spoiler</summary>\n" +
                "\n" +
                "```\n" +
                "Nested code\n" +
                "```\n" +
                "\n" +
                "</details>\n" +
                "</details>", actual);
        }

        [Fact]
        public void ShouldConvertAnchors()
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.Habrahabr,
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
