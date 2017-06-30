using NUnit.Framework;

namespace HabraMark.Tests
{
    [TestFixture]
    public class LinkTests
    {
        [Test]
        public void ConvertVisualCodeToGitHubRelativeLinks()
        {
            string expected =
                "* [header-2](#header-2)\n" +
                "    * [header-3](#header-3)\n" +
                "* [заголовок-2](#Заголовок-2)\n" +
                "    * [заголовок-3](#Заголовок-3)\n" +
                "    * [заголовок-header-3](#Заголовок-header-3)\n" +
                "* [missing-отсутствует](#missing-отсутствует)\n" +
                "\n" +
                "## Header 2\n" +
                "### Header 3\n" +
                "## Заголовок 2\n" +
                "### Заголовок 3\n" +
                "### Заголовок Header 3";

            Compare("RelativeLinks.VisualCode.md", expected, MarkdownType.VisualCode, MarkdownType.GitHub);
        }

        [Test]
        public void ConvertVisualCodeToHabrahabrRelativeLinks()
        {
            string expected =
                "* [header-2](#header-2)\n" +
                "    * [header-3](#header-3)\n" +
                "* [заголовок-2](#zagolovok-2)\n" +
                "    * [заголовок-3](#zagolovok-3)\n" +
                "    * [заголовок-header-3](#zagolovok-header-3)\n" +
                "* [missing-отсутствует](#missing-otsutstvuet)\n" +
                "\n" +
                "## Header 2\n" +
                "### Header 3\n" +
                "## Заголовок 2\n" +
                "### Заголовок 3\n" +
                "### Заголовок Header 3";

            Compare("RelativeLinks.VisualCode.md", expected, MarkdownType.VisualCode, MarkdownType.Habrahabr);
        }

        [Test]
        public void ConvertGitHubToHabrahabrRelativeLinks()
        {
            string expected =
                "* [header-2](#header-2)\n" +
                "    * [header-3](#header-3)\n" +
                "    * [header-3-1](#header-3-1)\n" +
                "    * [header-3-1-1](#header-3-1-1)\n" +
                "* [Заголовок-2](#zagolovok-2)\n" +
                "    * [Заголовок-3](#zagolovok-3)\n" +
                "    * [Заголовок-3-1](#zagolovok-3-1)\n" +
                "    * [Заголовок-3-1-1](#zagolovok-3-1-1)\n" +
                "    * [ЗАГОЛОВОК-3](#zagolovok-3-2)\n" +
                "    * [Заголовок-header-3](#zagolovok-header-3)\n" +
                "* [Missing-Пропущенный](#missing-propuschennyy)\n" +
                "\n" +
                "## Header 2\n" +
                "### Header 3\n" +
                "### Header 3\n" +
                "### Header 3 1\n" +
                "\n" +
                "## Заголовок 2\n" +
                "### Заголовок 3\n" +
                "### Заголовок 3\n" +
                "### Заголовок 3 1\n" +
                "### ЗАГОЛОВОК 3\n" +
                "### Заголовок Header 3";

            Compare("RelativeLinks.GitHub.md", expected, MarkdownType.GitHub, MarkdownType.Habrahabr);
        }

        [Test]
        public void ConvertHabrahabrToGitHubRelativeLinks()
        {
            string expected =
                "* [header-2](#header-2)\n" +
                "    * [header-3](#header-3)\n" +
                "    * [header-3-1](#header-3-1)\n" +
                "* [zagolovok-2](#Заголовок-2)\n" +
                "    * [zagolovok-3](#Заголовок-3)\n" +
                "    * [zagolovok-3-1](#ЗАГОЛОВОК-3-1)\n" +
                "    * [zagolovok-header-3](#Заголовок-header-3)\n" +
                "* [missing-propuschennyy](#missing-propuschennyy)\n" +
                "\n" +
                "## Header 2\n" +
                "### Header 3\n" +
                "### Header 3\n" +
                "## Заголовок 2\n" +
                "### Заголовок 3\n" +
                "### ЗАГОЛОВОК 3 1\n" +
                "### Заголовок Header 3";

            Compare("RelativeLinks.Habrahabr.md", expected, MarkdownType.Habrahabr, MarkdownType.GitHub);
        }

        [Test]
        public void ShouldNotChangeLinksInsideCodeSection()
        {
            string expected =
                "[header](#заголовок)\n" +
                "```\n" +
                "[header](#ЗАГОЛОВОК)\n" +
                "```\n" +
                "[header](#заголовок)\n" +
                "## ЗАГОЛОВОК\n" +
                "```\n" +
                "[header](#ЗАГОЛОВОК)\n" +
                "```";

            Compare("RelativeLinksAndCode.md", expected, MarkdownType.GitHub, MarkdownType.VisualCode);
        }

        [Test]
        public void GenerateHabrahabrLink()
        {
            string header = @"АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ  ABCabc    0123456789!""№;%:?*() -+=`~<>@#$^&[]{}\/|'_";
            string habraLink = Header.HeaderToHabralink(header);
            Assert.AreEqual(@"abvgdeyozhziyklmnoprstufhcchshschyeyuya--abcabc----0123456789--_", habraLink);
        }

        [Test]
        public void GenerateVisualCodeLink()
        {
            string header = @"ABCabc АБВгде    0123456789!""№;%:?*() -+=`~<>@#$^&[]{}\/|'_";
            string resultLink = Header.HeaderToLink(header, true);
            Assert.AreEqual(@"abcabc-абвгде----0123456789--_", resultLink);
        }

        [Test]
        public void GenerateGitHubLink()
        {
            string header = @"ABCabc АБВгде    0123456789!""№;%:?*() -+=`~<>@#$^&[]{}\/|'_";
            string resultLink = Header.HeaderToLink(header, false);
            Assert.AreEqual(@"abcabc-АБВгде----0123456789--_", resultLink);
        }

        [Test]
        public void ShouldAddHeaderImageLink()
        {
            var options = new ProcessorOptions { HeaderImageLink = "https://github.com/KvanTTT/HabraMark" };
            var processor = new Processor(options);
            string actual = processor.Process(
                "# Header\n" +
                "\n" +
                "Paragraph [Some link](https://google.com)\n" +
                "\n" +
                "![Header Image](https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg)");

            Assert.AreEqual("# Header\n" +
                "\n" +
                "Paragraph [Some link](https://google.com)\n" +
                "\n" +
                "[![Header Image](https://hsto.org/storage3/20b/eb7/170/20beb7170a61ec7cf9f4c02f8271f49c.jpg)](https://github.com/KvanTTT/HabraMark)",
                actual);
        }

        [Test]
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

            Assert.AreEqual(
                "Paragraph text\n" +
                "\n" +
                "## Header 2", actual);
        }

        private void Compare(string inputFileName, string outputResult, MarkdownType inputKind, MarkdownType outputKind)
        {
            var options = new ProcessorOptions
            {
                InputMarkdownType = inputKind,
                OutputMarkdownType = outputKind
            };

            var processor = new Processor(options);
            string source = Utils.ReadFileFromProject(inputFileName);
            string actual = processor.Process(source);

            Assert.AreEqual(outputResult, actual);
        }
    }
}
