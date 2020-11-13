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

        [Fact]
        public void ShouldEscapeHtmlComments()
        {
            var options = new ProcessorOptions();
            var processor = new Processor(options);

            var actual = processor.Process(
                                        "``\n" +
                                           "\n" +
                                           "`<!--`\n" +
                                           "\n" +
                                           "```\n" +
                                           "-->\n" +
                                           "```\n");

            Assert.Equal("``\n" +
                         "\n" +
                         "`<!--`\n" +
                         "\n" +
                         "```\n" +
                         "-->\n" +
                         "```", actual);
        }

        [Fact]
        public void ShouldWarnIncorrectHeaderLevel()
        {
            var options = new ProcessorOptions();
            var logger = new Logger();
            var processor = new Processor(options) { Logger = logger };
            string actual = processor.Process(
                "## Header 2\n" +
                "### Header 3\n" +
                "### Header 3 1\n" +
                "## Header 2 1\n" +
                "# Header 1\n");

            Assert.Single(logger.WarningMessages);
        }
    }
}
