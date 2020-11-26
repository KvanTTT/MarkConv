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
        public void ShouldCorrectlyMergeLines()
        {
            var processor = new Processor(new ProcessorOptions { LinesMaxLength = -1 }, new Logger());
            var result = processor.Process(@"a,
b,
c.
[a
b](https://google.com)
Image:
![Habr logo](https://habrastorage.org/webt/cf/ei/1k/cfei1ka04yu5e021ovuhsrlsr-s.png)

<b>text1
   text2</b>

<details>
text1
    text2
</details>");

            Assert.Equal(@"a, b, c. [a b](https://google.com) Image: ![Habr logo](https://habrastorage.org/webt/cf/ei/1k/cfei1ka04yu5e021ovuhsrlsr-s.png)

<b> text1 text2</b>

<details>text1 text2
</details>", result);
        }

        [Fact]
        public void ShouldNotInsertWhitespacesBeforePunctuation()
        {
            var processor = new Processor(new ProcessorOptions { LinesMaxLength = -1 }, new Logger());
            var result = processor.Process("[link](http://google.com), text");
            Assert.Equal("[link](http://google.com), text", result);
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
