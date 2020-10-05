using System;
using System.IO;

namespace MarkdigParserTest
{
    static class Program
    {
        static void Main(string[] args)
        {
            string fileOrDirectory = args[0];

            string[] files = Directory.Exists(fileOrDirectory)
                ? Directory.GetFiles(fileOrDirectory, "*.md", SearchOption.AllDirectories)
                : new[] {fileOrDirectory};

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            foreach (string file in files)
            {
                var origin = File.ReadAllText(file);

                var converter = new MarkdownVisitor();
                var result = converter.Convert(origin);

                File.WriteAllText(Path.Combine(baseDirectory, Path.GetFileName(file)), result);
            }
        }
    }
}