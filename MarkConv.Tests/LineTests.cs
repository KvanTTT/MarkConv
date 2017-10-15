using Xunit;

namespace MarkConv.Tests
{
    public class LineTests
    {
        [Fact]
        public void ShouldNotSplitSpecialLines()
        {
            Compare(4, "NotSplittingLines.md", "NotSplittingLines.Wrapped.md");
        }

        [Fact]
        public void ShouldNotChangeLines()
        {
            Compare(0, "SpecialLines.md", "SpecialLines.md");
        }

        [Fact]
        public void ShouldUnwrap()
        {
            Compare(-1, "SpecialLines.md", "SpecialLines.Unwrapped.md");
        }

        [Fact]
        public void ShouldWrapToDefinedWidth()
        {
            Compare(80, "SpecialLines.md", "SpecialLines.Wrapped.80.md");
        }

        [Fact]
        public void ShouldNormalizeLineBreaks()
        {
            var options = new ProcessorOptions { NormalizeBreaks = true };
            var processor = new Processor(options);
            string actual = processor.Process(
                "\n" +
                "# Header\n" +
                "\n" +
                "\n" +
                "Paragraph\n" +
                "## Header 2\n" +
                "Paragraph 2\n" +
                "\n" +
                "\n");

            Assert.Equal(
                "# Header\n" +
                "\n" +
                "Paragraph\n" +
                "\n" +
                "## Header 2\n" +
                "\n" +
                "Paragraph 2"
                , actual);
        }

        [Fact]
        public void ShouldNotRemoveUnwantedLineBreaks()
        {
            var options = new ProcessorOptions { NormalizeBreaks = false };
            var processor = new Processor(options);
            string actual = processor.Process(
                "\n" +
                "# Header\n" +
                "\n" +
                "\n" +
                "Paragraph\n" +
                "\n" +
                "\n");

            Assert.Equal(
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
            var options = new ProcessorOptions
            {
                LinesMaxLength = lineMaxLength,
                Normalize = false,
                NormalizeBreaks = false
            };
            var processor = new Processor(options);
            string source = Utils.ReadFileFromProject(sourceFileName);
            string actual = processor.Process(source);
            string expected = Utils.ReadFileFromProject(expectedFileName);

            Assert.Equal(expected, actual);
        }
    }
}
