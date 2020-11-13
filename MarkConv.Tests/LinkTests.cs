using System.IO;
using System.Linq;
using Xunit;

namespace MarkConv.Tests
{
    public class LinkTests : TestsBase
    {
        [Fact]
        public void ShouldCollectLinksFromHtmlAndMarkdown()
        {
            var logger = new Logger();
            var parser = new HtmlMarkdownParser(new ProcessorOptions(), logger);
            var parseResult = parser.ParseHtmlMarkdown(ReadFileFromResources("Links.md"));
            var links = parseResult.Links;
            Assert.Equal("https://google.com", links.ElementAt(0).Value.Address);
            Assert.Equal("https://habrastorage.org/web/dcd/2e2/016/dcd2e201667847a1932eab96b60c0086.jpg", links.ElementAt(1).Value.Address);
            Assert.Equal("header", links.ElementAt(2).Value.Address);
            Assert.Equal("https://raw.githubusercontent.com/lunet-io/markdig/master/img/markdig.png", links.ElementAt(3).Value.Address);
            Assert.Equal("https://github.com/lunet-io/markdig", links.ElementAt(4).Value.Address);
            Assert.Equal("https://twitter.com/", links.ElementAt(5).Value.Address);
            Assert.Equal("https://example.com/", links.ElementAt(6).Value.Address);
            Assert.Equal("https://stackoverflow.com/", links.ElementAt(7).Value.Address);
            Assert.Equal("https://github.com/KvanTTT/MarkConv", links.ElementAt(8).Value.Address);
            Assert.Equal("https://habrastorage.org/web/4bf/3c9/eaf/4bf3c9eaffe447ccb472240698033d3f.png", links.ElementAt(9).Value.Address);
        }

        [Fact]
        public void CheckAliveUrls()
        {
            var logger = new Logger();
            var parser = new HtmlMarkdownParser(new ProcessorOptions(), logger);
            var textFile = new TextFile(@"<https://github.com/KvanTTT/MarkConv>
<https://github.com/KvanTTT/MarkConv1>
[Correct Header](#header)
[Broken Header](#broken-header)

# Header
", "Links.md");
            var parseResult = parser.ParseHtmlMarkdown(textFile);
            var checker = new Checker(logger);
            checker.Check(parseResult);
            Assert.Equal(2, logger.WarningMessages.Count);
        }

        [Theory]
        [InlineData(MarkdownType.GitHub, MarkdownType.Habr)]
        [InlineData(MarkdownType.Habr, MarkdownType.GitHub)]
        public void ShouldConvertRelativeLinks(MarkdownType inMarkdownType, MarkdownType outMarkdownType)
        {
            Compare($"RelativeLinks.{inMarkdownType}.md", $"RelativeLinks.{outMarkdownType}.md",
                inMarkdownType, outMarkdownType);
        }

        [Fact]
        public void ShouldNotChangeAnythingInsideCodeSection()
        {
            Compare("RelativeLinksAndCode.md", "RelativeLinksAndCode-Converted.md",
                MarkdownType.GitHub, MarkdownType.GitHub);
        }

        [Fact]
        public void GenerateHabrLinkFromHeader()
        {
            string header = @"АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ  ABCabc    0123456789!""№;%:?*() -+=`~<>@#$^&[]{}\/|'_";
            string habraLink = HeaderToLinkConverter.ConvertHeaderTitleToLink(header, MarkdownType.Habr);
            Assert.Equal(@"abvgdeyozhziyklmnoprstufhcchshschyeyuya--abcabc----0123456789--_", habraLink);
        }

        [Fact]
        public void GenerateGitHubLinkFromHeader()
        {
            string header = @"ABCabc АБВгде    0123456789!""№;%:?*() -+=`~<>@#$^&[]{}\/|'_";
            string resultLink = HeaderToLinkConverter.ConvertHeaderTitleToLink(header, MarkdownType.GitHub);
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
                ImagesMap = ImagesMap.Load(Path.Combine(ProjectDir, "ImagesMap"), ProjectDir, logger),
                RootDirectory = ProjectDir
            };

            CompareFiles("Images.md", "Images-Mapped.md", options, logger);

            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("Duplicated")));
            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("Incorrect mapping")));
            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("File Invalid.png does not exist")));
            Assert.Equal(1, logger.WarningMessages.Count(message => message.Contains("Replacement link")));
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
            CompareFiles(inputFileName, outputFileName, options, logger);
        }
    }
}
