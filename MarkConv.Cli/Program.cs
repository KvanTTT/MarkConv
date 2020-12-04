using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MarkConv.Cli
{
    static class Program
    {
        private const string MdExtension = ".md";
        private const string OutExtension = ".out";
        private const string IgnoreExtension = ".ignore";

        private static ILogger Logger = new ConsoleLogger();

        static int Main(string[] args)
        {
            var parser = new CliParametersParser<CliParameters>(Logger);
            var parserResult = parser.Parse(args);

            if (parserResult.ShowHelp || Logger.ErrorCount > 0)
            {
                var helpText = CliParametersParser<CliParameters>.GenerateHelpText();
                foreach (string line in helpText)
                    Console.WriteLine(line);

                if (Logger.ErrorCount > 0)
                {
                    return 1;
                }
            }

            if (parserResult.ShowVersion)
            {
                Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
            }

            return Convert(parserResult.Parameters);
        }

        private static int Convert(CliParameters parameters)
        {
            string inputFileOrDirectory =
                parameters.InputFileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            string[] inputFiles;
            string? inputDirectory = null;
            string? outputDirectory = parameters.OutputDirectory;

            if (Directory.Exists(inputFileOrDirectory))
            {
                string outMdSuffix = OutExtension + MdExtension;
                string ignoreMdSuffix = IgnoreExtension + MdExtension;
                inputFiles = Directory.GetFiles(inputFileOrDirectory, "*" + MdExtension, SearchOption.AllDirectories)
                    .Where(file => !file.EndsWith(outMdSuffix, StringComparison.OrdinalIgnoreCase) && !file.EndsWith(ignoreMdSuffix, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                inputDirectory = inputFileOrDirectory;
                outputDirectory ??= Path.Combine(inputFileOrDirectory, "_Output");
            }
            else
            {
                inputFiles = new[] {inputFileOrDirectory};
            }

            var parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = 1};
            var options = ProcessorOptions.GetDefaultOptions(parameters.InputMarkdownType, parameters.OutputMarkdownType);

            if (parameters.CheckLinks.HasValue)
                options.CheckLinks = parameters.CheckLinks.Value;

            if (parameters.LinesMaxLength.HasValue)
                options.LinesMaxLength = parameters.LinesMaxLength.Value;

            if (parameters.RemoveTitleHeader.HasValue)
                options.RemoveTitleHeader = parameters.RemoveTitleHeader.Value;

            if (parameters.RemoveSpoilers.HasValue)
                options.RemoveDetails = parameters.RemoveSpoilers.Value;

            if (parameters.RemoveComments.HasValue)
                options.RemoveComments = parameters.RemoveComments.Value;

            Parallel.ForEach(inputFiles, parallelOptions, inputFile =>
            {
                try
                {
                    ConvertFile(inputFile, outputDirectory, inputDirectory, options);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error during {inputFile} processing: {ex.Message}");
                }
            });

            Logger.Info("Process completed");

            return Logger.ErrorCount == 0 ? 0 : 2;
        }

        private static void ConvertFile(string inputFile, string? outputDirectory,
            string? inputDirectory, ProcessorOptions options)
        {
            Logger.Info($"Converting of file {Path.GetFullPath(inputFile)}...");

            string directory = Path.GetDirectoryName(inputFile) ?? "";
            string fileName = Path.GetFileNameWithoutExtension(inputFile);

            var file = TextFile.Read(inputFile);

            var processor = new Processor(options, Logger);
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

            Logger.Info($"File {outputFileName} is ready");
            Logger.Info("");
        }
    }
}