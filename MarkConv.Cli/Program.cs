using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace MarkConv.Cli
{
    static class Program
    {
        private const string MdExtension = ".md";
        private const string OutExtension = ".out";

        static int Main(string[] args)
        {
            var parser = new CommandLine.Parser(config => config.HelpWriter = Console.Out);
            var parserResult = parser.ParseArguments<CliParameters>(args);
            return parserResult.MapResult(Convert, ProcessErrors);
        }

        private static int Convert(CliParameters parameters)
        {
            string inputFileOrDirectory = parameters.InputFileName;

            string[] inputFiles;
            string inputDirectory = null;
            string outputDirectory = parameters.OutputDirectory;

            if (Directory.Exists(inputFileOrDirectory))
            {
                string outMdSuffix = OutExtension + MdExtension;
                inputFiles = Directory.GetFiles(inputFileOrDirectory, "*" + MdExtension, SearchOption.AllDirectories)
                    .Where(file => !file.EndsWith(outMdSuffix, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                inputDirectory = inputFileOrDirectory;
                outputDirectory ??= Path.Combine(inputFileOrDirectory, "_Output");
            }
            else
            {
                inputFiles = new[] {inputFileOrDirectory};
            }

            var parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = -1};
            var logger = new ConsoleLogger();

            Parallel.ForEach(inputFiles, parallelOptions, inputFile =>
            {
                try
                {
                    ConvertFile(parameters, inputFile, outputDirectory, inputDirectory, logger);
                }
                catch (Exception ex)
                {
                    logger.Warn($"Error during {inputFile} processing: {ex.Message}");
                }
            });

            return 0;
        }

        private static void ConvertFile(CliParameters parameters, string inputFile, string outputDirectory,
            string inputDirectory, ILogger logger)
        {
            logger.Info($"Converting of file {inputFile}...");

            string directory = Path.GetDirectoryName(inputFile) ?? "";
            string fileName = Path.GetFileNameWithoutExtension(inputFile);
            string fileNameWoExt = Path.GetFileNameWithoutExtension(fileName);

            var file = TextFile.Read(inputFile);
            var options = ProcessorOptions.GetDefaultOptions(parameters.InputMarkdownType, parameters.OutputMarkdownType);
            options.CheckLinks = parameters.CheckLinks;
            options.CompareImageHashes = parameters.CompareImages;

            if (parameters.LinesMaxLength.HasValue)
                options.LinesMaxLength = parameters.LinesMaxLength.Value;

            options.RootDirectory = directory;

            options.ImagesMap = ImagesMap.Load(parameters.ImagesMapFileName, directory, logger);

            if (parameters.HeaderImageLink != null)
                options.HeaderImageLink = parameters.HeaderImageLink;
            else if (options.ImagesMap.TryGetValue(fileNameWoExt + ImagesMap.HeaderImageLinkSrc, out Image image))
                options.HeaderImageLink = image.Address;

            if (parameters.RemoveTitleHeader.HasValue)
                options.RemoveTitleHeader = parameters.RemoveTitleHeader.Value;

            options.RemoveDetails = parameters.RemoveSpoilers;
            options.RemoveComments = parameters.RemoveComments;

            var processor = new Processor(options, logger);
            var converted = processor.Process(file);

            string localOutputDirectory = outputDirectory ?? directory;

            string outputFileName = fileName;
            if (inputDirectory != null)
            {
                if (directory.StartsWith(inputDirectory))
                {
                    int index = inputDirectory.Length;
                    if (index + 1 < directory.Length && directory[index] == Path.DirectorySeparatorChar)
                    {
                        index++;
                    }

                    string relativePath = directory.Substring(index).Replace(Path.DirectorySeparatorChar, '.');
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        relativePath += ".";
                    }

                    outputFileName = relativePath + outputFileName;
                }
            }

            outputFileName =
                $"{outputFileName}.{options.OutputMarkdownType.ToString().ToLowerInvariant()}{OutExtension}{MdExtension}";

            if (!Directory.Exists(localOutputDirectory))
            {
                Directory.CreateDirectory(localOutputDirectory);
            }

            File.WriteAllText(Path.Combine(localOutputDirectory, outputFileName), converted);

            logger.Info($"File {outputFileName} is ready");
        }

        private static int ProcessErrors(IEnumerable<Error> errors) => 1;
    }
}