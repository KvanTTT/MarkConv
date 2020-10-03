using System.IO;
using System.Linq;
using Xunit;

namespace MarkConv.Tests
{
    public class LinkTests
    {
        [Fact]
        public void ConvertVisualCodeToHabrRelativeLinks()
        {
            Compare("RelativeLinks.Common.md", "RelativeLinks.Common-to-Habr.md",
                MarkdownType.Common, MarkdownType.Habr);
        }

        [Fact]
        public void ConvertHabrToCommonRelativeLinks()
        {
            Compare("RelativeLinks.Habr.md", "RelativeLinks.Habr-to-Common.md",
                MarkdownType.Habr, MarkdownType.Common);
        }

        [Fact]
        public void ShouldNotChangeAnythingInsideCodeSection()
        {
            Compare("RelativeLinksAndCode.md", "RelativeLinksAndCode-Converted.md",
                MarkdownType.Common, MarkdownType.Common);
        }

        [Fact]
        public void GenerateHabrLink()
        {
            string header = @"АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ  ABCabc    0123456789!""№;%:?*() -+=`~<>@#$^&[]{}\/|'_";
            string habraLink = Header.HeaderToTranslitLink(header);
            Assert.Equal(@"abvgdeyozhziyklmnoprstufhcchshschyeyuya--abcabc----0123456789--_", habraLink);
        }

        [Fact]
        public void GenerateVisualCodeLink()
        {
            string header = @"ABCabc АБВгде    0123456789!""№;%:?*() -+=`~<>@#$^&[]{}\/|'_";
            string resultLink = Header.HeaderToLink(header);
            Assert.Equal(@"abcabc-абвгде----0123456789--_", resultLink);
        }

        [Fact]
        public void ShouldAddHeaderImageLink()
        {
            var options = new ProcessorOptions { HeaderImageLink = "https://github.com/KvanTTT/MarkConv" };
            var processor = new Processor(options);
            string actual = processor.Process(
                "# Header\n" +
                "\n" +
                "Paragraph [Some link](https://google.com)\n" +
                "\n" +
                "![Header Image](https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg)");

            Assert.Equal("# Header\n" +
                "\n" +
                "Paragraph [Some link](https://google.com)\n" +
                "\n" +
                "[![Header Image](https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg)](https://github.com/KvanTTT/MarkConv)",
                actual);
        }

        [Fact]
        public void ShouldRemoveFirstLevelHeader()
        {
            var options = new ProcessorOptions { RemoveTitleHeader = true };
            var processor = new Processor(options);
            string actual = processor.Process(
                "# Header\n" +
                "\n" +
                "Paragraph text\n" +
                "\n" +
                "## Header 2");

            Assert.Equal(
                "Paragraph text\n" +
                "\n" +
                "## Header 2", actual);
        }

        [Fact]
        public void ShouldMapImageLinks()
        {
            var logger = new Logger();
            var options = new ProcessorOptions
            {
                CheckLinks = true,
                ImagesMap = ImagesMap.Load(Path.Combine(Utils.ProjectDir, "ImagesMap"), Utils.ProjectDir, logger),
                RootDirectory = Utils.ProjectDir
            };

            Utils.CompareFiles("Images.md", "Images-Mapped.md", options, logger);

            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("Duplicated")));
            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("Incorrect mapping")));
            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("File Invalid.png does not exist")));
            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("Replacement link")));
        }

        [Fact]
        public void CheckAliveUrls()
        {
            Assert.True(Link.IsUrlAlive("https://github.com/KvanTTT/MarkConv"));
            Assert.False(Link.IsUrlAlive("https://github.com/KvanTTT/MarkConv1"));
        }

        private void Compare(string inputFileName, string outputFileName, MarkdownType inputKind, MarkdownType outputKind)
        {
            var options = new ProcessorOptions
            {
                InputMarkdownType = inputKind,
                OutputMarkdownType = outputKind,
                NormalizeBreaks = false
            };

            var logger = new Logger();
            Utils.CompareFiles(inputFileName, outputFileName, options, logger);

            Assert.Single(logger.WarningMessages);
        }
    }
}
