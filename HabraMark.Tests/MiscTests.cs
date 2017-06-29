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
    }
}
