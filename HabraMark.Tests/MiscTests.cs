using System.Collections.Generic;
using Xunit;

namespace HabraMark.Tests
{
    public class MiscTests
    {
        [Fact]
        public void ShouldNormalize()
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
                "25. Ordered list item\n" +
                "```single-line-code```");

            Assert.Equal(
                "## Head # er\n" +
                "## Header 2\n" +
                "* List item 1\n" +
                "* List item 2\n" +
                "* List item 3\n" +
                "25. Ordered list item\n" +
                "`single-line-code`", actual);
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

            Assert.Equal(1, logger.WarningMessages.Count);
        }

        [Fact]
        public void ShouldGenerateTableOfContents()
        {
            string source = Utils.ReadFileFromProject("RelativeLinks.GitHub.md");

            var linesProcessor = new LinesProcessor();
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
                "    * [ЗАГОЛОВОК 3](#заголовок-3-2)\n" +
                "    * [Заголовок Header 3](#заголовок-header-3)", actual);
        }
    }
}
