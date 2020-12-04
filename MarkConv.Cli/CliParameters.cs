namespace MarkConv.Cli
{
    public class CliParameters
    {
        [Option('f', "file", Required = true, HelpText = "Input file to be processed")]
        public string InputFileName { get; set; } = "";

        [Option('i', "inputType", HelpText = "Markdown type of an input image (GitHub, Habr, Dev)")]
        public MarkdownType? InputMarkdownType { get; set; }

        [Option('o', "outputType", HelpText = "Markdown type of an output image (GitHub, Habr, Dev)")]
        public MarkdownType? OutputMarkdownType { get; set; }

        [Option('l', "linesLength", HelpText = "Lines max length. 0 - not change, -1 - merge lines")]
        public int? LinesMaxLength { get; set; }

        [Option("outDir")]
        public string? OutputDirectory { get; set; }

        [Option]
        public bool? RemoveTitleHeader { get; set; }

        [Option]
        public bool? CheckLinks { get; set; }

        [Option]
        public bool? RemoveSpoilers { get; set; }

        [Option]
        public bool? RemoveComments { get; set; }

        [Option("errorCode", HelpText = "Return not zero error code if errors occured (use for CI)")]
        public bool NotZeroErrorCodeIfErrors { get; set; }
    }
}
