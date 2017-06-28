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
            var options = new ProcessorOptions
            {
                LinesMaxLength = -1,
                RemoveTitleHeader = false,
                OutputRelativeLinksKind = RelativeLinksKind.VisualCode,
                HeaderImageLink = "",
                ReplaceSpoilers = true,
                RemoveUnwantedBreaks = true
            };
            var processor = new Processor(options);
            var converted = processor.Process(data);

            File.WriteAllText(Path.Combine(directory, $"{fileName}_habr.md"), converted);
        }
    }
}