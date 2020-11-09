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

        public static string ReadFileFromResources(string fileName)
        {
            return File.ReadAllText(Path.Combine(ProjectDir, "Resources", fileName));
        }

        private static void InitProjectDir([CallerFilePath]string thisFilePath = null)
        {
            ProjectDir = Path.GetDirectoryName(thisFilePath);
        }

        public static void CompareFiles(string inputFileName, string outputFileName, ProcessorOptions options = null,
            Logger logger = null)
        {
            var processor = new Processor(options);
            processor.Logger = logger;
            string source = ReadFileFromResources(inputFileName);
            string actual = processor.Process(source).Replace("\r\n", "\n");
            string expected = ReadFileFromResources(outputFileName).Replace("\r\n", "\n");

            Assert.Equal(expected, actual);
        }
    }
}