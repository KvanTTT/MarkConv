using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace MarkConv.Tests
{
    public abstract class TestsBase
    {
        protected static string ProjectDir { get; private set; }

        static TestsBase()
        {
            InitProjectDir();
        }

        private static void InitProjectDir([CallerFilePath]string thisFilePath = null)
        {
            ProjectDir = Path.GetDirectoryName(thisFilePath);
        }

        protected static void CompareFiles(string inputFileName, string outputFileName, ProcessorOptions options = null,
            Logger logger = null)
        {
            var processor = new Processor(options, logger ?? new Logger());
            string actual = processor.Process(ReadFileFromResources(inputFileName));
            string expected = ReadFileFromResources(outputFileName).Data;

            Assert.Equal(expected, actual);
        }

        protected static TextFile ReadFileFromResources(string fileName)
        {
            var fullPath = Path.Combine(ProjectDir, "Resources", fileName);
            return TextFile.Read(fullPath);
        }
    }
}