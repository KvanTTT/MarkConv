using Xunit;

namespace MarkConv.Tests
{
    public class MiscTests : TestsBase
    {
        [Theory]
        [InlineData("Headers")]
        [InlineData("Inlines")]
        [InlineData("Lists")]
        [InlineData("Quotes")]
        [InlineData("CodeBlocks")]
        [InlineData("Html")]
        public void ShouldConvertMarkdown(string fileName)
        {
            var options = new ProcessorOptions();
            fileName = $"{fileName}.md";
            CompareFiles(fileName, fileName, options);
        }
    }
}
