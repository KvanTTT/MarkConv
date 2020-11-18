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
            var processor = new Processor(new ProcessorOptions(), new Logger());

            Assert.Equal(
@"# Header

Paragraph

## Header 2

Paragraph 2"
                , processor.Process(
    @"
# Header


Paragraph
## Header 2
Paragraph 2


"));
        }

        private static void Compare(int lineMaxLength, string sourceFileName, string expectedFileName)
        {
            var options = new ProcessorOptions { LinesMaxLength = lineMaxLength };
            CompareFiles(sourceFileName, expectedFileName, options);
        }
    }
}
