using System.Collections.Generic;
using Xunit;

namespace MarkConv.Tests
{
    public class MiscTests
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
            Utils.CompareFiles(fileName, fileName, options);
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

        [Fact]
        public void ShouldGenerateTableOfContents()
        {
            string source = Utils.ReadFileFromResources("RelativeLinks.Common.md");

            var options = new ProcessorOptions
                {InputMarkdownType = MarkdownType.GitHub, OutputMarkdownType = MarkdownType.GitHub};
            var linesProcessor = new LinesProcessor(options);
            LinesProcessorResult linesProcessorResult = linesProcessor.Process(source);
            List<string> tableOfContents = linesProcessor.GenerateTableOfContents(linesProcessorResult);
            string actual = string.Join("\n", tableOfContents);

            Assert.Equal(
                "* [Header 2](#header-2)\n" +
                "    * [Header 3](#header-3)\n" +
                "    * [Header 3](#header-3-1)\n" +
                "    * [Header 3 1](#header-3-1-1)\n" +
                "    * [Header 3 1](#header-3-1-2)\n" +
                "* [Заголовок 2](#заголовок-2)\n" +
                "    * [Заголовок 3](#заголовок-3)\n" +
                "    * [Заголовок 3](#заголовок-3-1)\n" +
                "    * [Заголовок 3 1](#заголовок-3-1-1)\n" +
                "    * [Заголовок Header 3](#заголовок-header-3)\n" +
                "* [Header With Link](#header-with-link)", actual);
        }
    }
}
