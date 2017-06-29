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
        public void ShouldConvertDetailsElementsToSpoilers()
        {
            var options = new ProcessorOptions { LinesMaxLength = 0, ReplaceSpoilers = true };
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
    }
}
