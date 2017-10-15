using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace MarkConv.Tests
{
    public static class Utils
    {
        public static string ProjectDir { get; private set; }

        static Utils()
        {
            InitProjectDir();
        }

        public static string ReadFileFromProject(string fileName)
        {
            return File.ReadAllText(Path.Combine(ProjectDir, fileName));
        }

        private static void InitProjectDir([CallerFilePath]string thisFilePath = null)
        {
            ProjectDir = Path.GetDirectoryName(thisFilePath);
        }

        public static void CompareFiles(string inputFileName, string outputFileName, ProcessorOptions options,
            Logger logger = null)
        {
            var processor = new Processor(options) { Logger = logger };
            string source = ReadFileFromProject(inputFileName);
            string actual = processor.Process(source);
            string expected = ReadFileFromProject(outputFileName);

            Assert.Equal(expected, actual);
        }
    }
}
