using NUnit.Framework;

namespace HabraMark.Tests
{
    [TestFixture]
    public class MiscTests
    {
        [Test]
        public void ShouldNormalizeHeadersAndListItems()
        {
            var options = new ProcessorOptions { Normalize = true };
            var processor = new Processor(options);
            string actual = processor.Process(
                " ##   Head # er   ##  \n" +
                "Header 2\n" +
                "---\n" +
                "* List item 1\n" +
                "+ List item 2\n" +
                "- List item 3\n" +
                "25. Ordered list item");

            Assert.AreEqual(
                "## Head # er\n" +
                "## Header 2\n" +
                "* List item 1\n" +
                "* List item 2\n" +
                "* List item 3\n" +
                "25. Ordered list item", actual);
        }

        [Test]
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
            Assert.AreEqual(
                "<spoiler title=\"Details\">\n" +
                "Content\n" +
                "\n" +
                "```\n" +
                "Some code\n" +
                "```\n" +
                "<spoiler title=\"Nested Details\">\n" +
                "Nested text\n" +
                "</spoiler>\n" +
                "</spoiler>", actual);
        }

        [Test]
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
            Assert.AreEqual(
                "<details>\n" +
                "<summary>Spoiler header</summary>\n" +
                "Content\n" +
                "\n" +
                "```\n" +
                "Some code\n" +
                "```\n" +
                "<details>\n" +
                "<summary>Nested spoiler</summary>\n" +
                "Nested text\n" +
                "</details>\n" +
                "</details>", actual);
        }
    }
}
