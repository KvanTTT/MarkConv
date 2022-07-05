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
            var parser = new Parser(new ProcessorOptions(), logger, ReadFileFromResources("Links.md"));
            var parseResult = parser.Parse();
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
            var options = new ProcessorOptions { CheckLinks = true };
            var textFile = new TextFile(@"<https://github.com/KvanTTT/MarkConv>
<https://github.com/KvanTTT/MarkConv1>
[Incorrect Link Format]((http://asdf.qwer))
[Incorrect Link Format 2]((http://zxcv.qwer))
[Correct Header](#header)
[Broken Header](#broken-header)
[Correct Link](http://ru.wikipedia.org/wiki/%D0%9F%D1%80%D0%B5%D0%BE%D0%B1%D1%80%D0%B0%D0%B7%D0%BE%D0%B2%D0%B0%D0%BD%D0%B8%D0%B5_%D0%A5%D0%B0%D1%84%D0%B0)
[Redirect from http to https](http://msdn.microsoft.com/en-us/library/bb933790.aspx)

# Header
", "Links.md");
            var parser = new Parser(options, logger, textFile);
            var parseResult = parser.Parse();
            var checker = new Checker(options, logger);
            checker.Check(parseResult);

            Assert.Equal(3, logger.WarningMessages.Count);
            Assert.Single(logger.ErrorMessages);
            Assert.Equal("Relative link broken-header at [6,1..32) is broken", logger.ErrorMessages[0]);
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
        public void ShouldConvertRelativeLinkInvariantCase()
        {
            var options = new ProcessorOptions { InputMarkdownType = MarkdownType.GitHub, OutputMarkdownType = MarkdownType.Habr };
            var logger = new Logger();
            var processor = new Processor(options, logger);
            processor.Process(@"[Заголовок](#Заголовок)

# Заголовок");

            Assert.Empty(logger.WarningMessages);
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
            var options = new ProcessorOptions();
            var processor = new Processor(options, new Logger());
            string actual = processor.Process(
@"<linkmap src=HeaderImageLink dst=https://github.com/KvanTTT/MarkConv />

# Header

Paragraph [Some link](https://google.com)

![Header Image](https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg)");

            Assert.Equal(
@"# Header

Paragraph [Some link](https://google.com)

[![Header Image](https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg)](https://github.com/KvanTTT/MarkConv)",
                actual);

            actual = processor.Process(
                @"<linkmap src=HeaderImageLink dst=https://github.com/KvanTTT/MarkConv />

# Header

Paragraph [Some link](https://google.com)

<img src=""https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg"" alt=""Header Image"">");

            Assert.Equal(
                @"# Header

Paragraph [Some link](https://google.com)

[<img src=""https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg"" alt=""Header Image"">](https://github.com/KvanTTT/MarkConv)",
                actual);
        }

        [Fact]
        public void ShouldRemoveFirstLevelHeader()
        {
            var processor = new Processor(new ProcessorOptions { RemoveTitleHeader = true }, new Logger());

            Assert.Equal(
@"Paragraph text

## Header 2",

processor.Process(@"# Header

Paragraph text

## Header 2"));
        }

        [Fact]
        public void ShouldMapLinks()
        {
            var logger = new Logger();
            var options = new ProcessorOptions
            {
                CheckLinks = true,
                RootDirectory = ProjectDir
            };

            CompareFiles("Images.md", "Images-Mapped.md", options, logger);

            var warnings = logger.WarningMessages;
            Assert.Equal("linkmap \"GitHub.jpg\" at [7,14..24) replaces linkmap at [3,14..24)", warnings[0]);
            Assert.Equal("Absolute Link https://habrastorage-1.org/not-existed.png at [6,30..74) is probably broken", warnings[1]);
            Assert.Equal(2, logger.ErrorMessages.Count(message => message.Contains("does not exist")));
        }

        private void Compare(string inputFileName, string outputFileName, MarkdownType inputKind, MarkdownType outputKind)
        {
            var options = new ProcessorOptions
            {
                InputMarkdownType = inputKind,
                OutputMarkdownType = outputKind
            };

            var logger = new Logger();
            CompareFiles(inputFileName, outputFileName, options, logger);
        }
    }
}
