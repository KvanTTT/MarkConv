using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace HabraMark.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            var parser = new Parser(config => config.HelpWriter = Console.Out);
            ParserResult<CliParameters> parserResult = parser.ParseArguments<CliParameters>(args);

            var result = parserResult.MapResult(
                cliParams => Convert(cliParams),
                errors => ProcessErrors(errors));

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }

            return result;
        }

        private static int Convert(CliParameters parameters)
        {
            string directory = Path.GetDirectoryName(parameters.InputFileName);
            string fileName = Path.GetFileNameWithoutExtension(parameters.InputFileName);

            string data = File.ReadAllText(parameters.InputFileName);
            var options = ProcessorOptions.GetDefaultOptions(parameters.InputMarkdownType, parameters.OutputMarkdownType);
            options.CheckLinks = parameters.CheckLinks;
            options.CompareImageHashes = parameters.CompareImages;

            if (parameters.LinesMaxLength.HasValue)
                options.LinesMaxLength = parameters.LinesMaxLength.Value;

            if (parameters.HeaderImageLink != null)
                options.HeaderImageLink = parameters.HeaderImageLink;

            if (parameters.RemoveTitleHeader.HasValue)
                options.RemoveTitleHeader = parameters.RemoveTitleHeader.Value;

            if (parameters.RemoveUnwantedBreaks.HasValue)
                options.NormalizeBreaks = parameters.RemoveUnwantedBreaks.Value;

            var logger = new ConsoleLogger();
            options.ImagesMap = ImagesMap.Load(parameters.ImagesMapFileName, directory, logger);
            options.RootDirectory = directory;

            var processor = new Processor(options) { Logger = logger };
            var converted = processor.ProcessAndGetTableOfContents(data);

            if (parameters.TableOfContents)
            {
                string tableOfContents = string.Join("\n", converted.TableOfContents);
                Console.WriteLine("Table of Contents:");
                Console.WriteLine(tableOfContents);
                File.WriteAllText(Path.Combine(directory, $"{fileName}-table-of-contents.md"), tableOfContents);
            }

            File.WriteAllText(Path.Combine(directory, $"{fileName}-{options.InputMarkdownType}-{options.OutputMarkdownType}.md"), converted.Result);

            return 0;
        }

        private static int ProcessErrors(IEnumerable<Error> errors)
        {
            return 1;
        }
    }
}