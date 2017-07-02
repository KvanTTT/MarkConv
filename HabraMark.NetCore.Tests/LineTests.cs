using NUnit.Framework;

namespace HabraMark.Tests
{
    [TestFixture]
    public class LineTests
    {
        [Test]
        public void ShouldNotSplitSpecialLines()
        {
            Compare(4, "NotSplittingLines.md", "NotSplittingLines.Wrapped.md");
        }

        [Test]
        public void ShouldNotChangeLines()
        {
            Compare(0, "SpecialLines.md", "SpecialLines.md");
        }

        [Test]
        public void ShouldUnwrap()
        {
            Compare(-1, "SpecialLines.md", "SpecialLines.Unwrapped.md");
        }

        [Test]
        public void ShouldWrapToDefinedWidth()
        {
            Compare(80, "SpecialLines.md", "SpecialLines.Wrapped.80.md");
        }

        [Test]
        public void ShouldRemoveUnwantedLineBreaks()
        {
            var options = new ProcessorOptions { RemoveUnwantedBreaks = true };
            var processor = new Processor(options);
            string actual = processor.Process(
                "\n" +
                "# Header\n" +
                "\n" +
                "\n" +
                "Paragraph\n" +
                "\n" +
                "\n");

            Assert.AreEqual(
                "# Header\n" +
                "\n" +
                "Paragraph"
                , actual);
        }

        [Test]
        public void ShouldNotRemoveUnwantedLineBreaks()
        {
            var options = new ProcessorOptions { RemoveUnwantedBreaks = false };
            var processor = new Processor(options);
            string actual = processor.Process(
                "\n" +
                "# Header\n" +
                "\n" +
                "\n" +
                "Paragraph\n" +
                "\n" +
                "\n");

            Assert.AreEqual(
                "\n" +
                "# Header\n" +
                "\n" +
                "\n" +
                "Paragraph\n" +
                "\n" +
                "\n"
                , actual);
        }

        private static void Compare(int lineMaxLength, string sourceFileName, string expectedFileName)
        {
            var options = new ProcessorOptions { LinesMaxLength = lineMaxLength, Normalize = false };
            var processor = new Processor(options);
            string source = Utils.ReadFileFromProject(sourceFileName);
            string actual = processor.Process(source);
            string expected = Utils.ReadFileFromProject(expectedFileName);

            Assert.AreEqual(expected, actual);
        }
    }
}
