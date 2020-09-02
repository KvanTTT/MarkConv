using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;

namespace MarkConv.Cli
{
    static class Program
    {
        static int Main(string[] args)
        {
            var parser = new Parser(config => config.HelpWriter = Console.Out);
            ParserResult<CliParameters> parserResult = parser.ParseArguments<CliParameters>(args);
            return parserResult.MapResult(Convert, ProcessErrors);
        }

        private static int Convert(CliParameters parameters)
        {
            string directory = Path.GetDirectoryName(parameters.InputFileName);
            string fileName = Path.GetFileNameWithoutExtension(parameters.InputFileName);
            string fileNameWoExt = Path.GetFileNameWithoutExtension(fileName);

            string data = File.ReadAllText(parameters.InputFileName);
            var options = ProcessorOptions.GetDefaultOptions(parameters.InputMarkdownType, parameters.OutputMarkdownType);
            options.CheckLinks = parameters.CheckLinks;
            options.CompareImageHashes = parameters.CompareImages;

            if (parameters.LinesMaxLength.HasValue)
                options.LinesMaxLength = parameters.LinesMaxLength.Value;

            var logger = new ConsoleLogger();
            options.RootDirectory = directory;

            options.ImagesMap = ImagesMap.Load(parameters.ImagesMapFileName, directory, logger);

            if (parameters.HeaderImageLink != null)
                options.HeaderImageLink = parameters.HeaderImageLink;
            else if (options.ImagesMap.TryGetValue(fileNameWoExt + ImagesMap.HeaderImageLinkSrc, out ImageHash imageHash))
                options.HeaderImageLink = imageHash.Path;

            if (parameters.RemoveTitleHeader.HasValue)
                options.RemoveTitleHeader = parameters.RemoveTitleHeader.Value;

            if (parameters.RemoveUnwantedBreaks.HasValue)
                options.NormalizeBreaks = parameters.RemoveUnwantedBreaks.Value;

            options.RemoveSpoilers = parameters.RemoveSpoilers;
            options.RemoveComments = parameters.RemoveComments;

            var processor = new Processor(options) { Logger = logger };
            var converted = processor.ProcessAndGetTableOfContents(data);

            if (parameters.TableOfContents)
            {
                string tableOfContents = string.Join("\n", converted.TableOfContents);
                Console.WriteLine("Table of Contents:");
                Console.WriteLine(tableOfContents);
                File.WriteAllText(Path.Combine(directory, $"{fileName}-table-of-contents.md"), tableOfContents);
            }

            string outputFileName = $"{fileName}-{options.InputMarkdownType}-to-{options.OutputMarkdownType}.md";
            File.WriteAllText(Path.Combine(directory, outputFileName), converted.Result);
            logger.Info($"File {outputFileName} is ready");

            return 0;
        }

        private static int ProcessErrors(IEnumerable<Error> errors) => 1;
    }
}