using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace MarkConv.Tests
{
    public abstract class TestsBase
    {
        public static string ProjectDir { get; private set; }

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
            var processor = new Processor(options) {Logger = logger ?? new Logger()};
            string source = ReadFileFromResources(inputFileName).Data;
            string actual = processor.Process(source);
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