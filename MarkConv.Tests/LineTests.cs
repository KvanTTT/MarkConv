using Xunit;

namespace MarkConv.Tests
{
    public class LineTests : TestsBase
    {
        [Fact]
        public void ShouldNotSplitSpecialLines()
        {
            Compare(4, "NotSplittingLines.md", "NotSplittingLines.Wrapped.md");
        }

        [Fact]
        public void ShouldNormalizeLineBreaks()
        {
            var options = new ProcessorOptions { NormalizeBreaks = true };
            var processor = new Processor(options, new Logger());
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

        private static void Compare(int lineMaxLength, string sourceFileName, string expectedFileName)
        {
            var options = new ProcessorOptions
            {
                LinesMaxLength = lineMaxLength,
                Normalize = false,
                NormalizeBreaks = false
            };

            CompareFiles(sourceFileName, expectedFileName, options);
        }
    }
}
