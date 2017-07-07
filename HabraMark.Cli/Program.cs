using CommandLine;
using System.IO;

namespace HabraMark.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            ParserResult<CliParameters> parserResult = Parser.Default.ParseArguments<CliParameters>(args);

            return parserResult.MapResult(
                cliParams => Convert(cliParams),
                errs => 1);
        }

        private static int Convert(CliParameters parameters)
        {
            string directory = Path.GetDirectoryName(parameters.InputFileName);
            string fileName = Path.GetFileNameWithoutExtension(parameters.InputFileName);

            var data = File.ReadAllText(parameters.InputFileName);
            var options = ProcessorOptions.GetDefaultOptions(parameters.InputMarkdownType, parameters.OutputMarkdownType);

            if (parameters.LinesMaxLength.HasValue)
                options.LinesMaxLength = parameters.LinesMaxLength.Value;

            if (parameters.HeaderImageLink != null)
                options.HeaderImageLink = parameters.HeaderImageLink;

            if (parameters.RemoveTitleHeader.HasValue)
                options.RemoveTitleHeader = parameters.RemoveTitleHeader.Value;

            if (parameters.RemoveUnwantedBreaks.HasValue)
                options.RemoveUnwantedBreaks = parameters.RemoveUnwantedBreaks.Value;

            var processor = new Processor(options) { Logger = new ConsoleLogger() };
            var converted = processor.ProcessAndGetTableOfContents(data);
            //string tableOfContents = string.Join("\n", converted.TableOfContents);

            File.WriteAllText(Path.Combine(directory, $"{fileName}-{options.InputMarkdownType}-{options.OutputMarkdownType}.md"), converted.Result);

            return 0;
        }
    }
}