using System.IO;
using System.Runtime.CompilerServices;

namespace HabraMark.Tests
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
    }
}
