using System.IO;

namespace HabraMark.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            string directory = Path.GetDirectoryName(args[0]);
            string fileName = Path.GetFileNameWithoutExtension(args[0]);

            var data = File.ReadAllText(args[0]);
            var processor = new Processor
            {
                LinesMaxLength = -1,
                RemoveTitleHeader = true,
                RelativeLinksKind = RelativeLinksKind.VisualCode,
                HeaderImageLink = "",
                ReplaceSpoilers = true,
                Trim = true
            };
            var converted = processor.Process(data);

            File.WriteAllText(Path.Combine(directory, $"{fileName}_habr.md"), converted);
        }
    }
}