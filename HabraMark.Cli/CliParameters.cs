using CommandLine;

namespace HabraMark.Cli
{
    public class CliParameters
    {
        [Option('f', "file", Required = true, HelpText = "Input file to be processed")]
        public string InputFileName { get; set; }

        [Option('i', "inputType")]
        public MarkdownType InputMarkdownType { get; set; } = MarkdownType.Default;

        [Option('o', "outputType")]
        public MarkdownType OutputMarkdownType { get; set; } = MarkdownType.Default;

        [Option('l', "linesLength", HelpText = "Lines max length. 0 - not change, -1 - merge lines")]
        public int? LinesMaxLength { get; set; } = null;

        [Option]
        public string HeaderImageLink { get; set; } = null;

        [Option]
        public bool? RemoveTitleHeader { get; set; } = null;

        [Option]
        public bool? RemoveUnwantedBreaks { get; set; } = null;

        [Option]
        public bool Normalize { get; set; } = false;
    }
}
