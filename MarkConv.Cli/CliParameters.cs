using CommandLine;

namespace MarkConv.Cli
{
    public class CliParameters
    {
        [Option('f', "file", Required = true, HelpText = "Input file to be processed")]
        public string InputFileName { get; set; } = "";

        [Option('i', "inputType", HelpText = "Markdown type of an input image (GitHub, Habr, Dev)")]
        public MarkdownType InputMarkdownType { get; set; } = MarkdownType.GitHub;

        [Option('o', "outputType", HelpText = "Markdown type of an output image (GitHub, Habr, Dev)")]
        public MarkdownType OutputMarkdownType { get; set; } = MarkdownType.Habr;

        [Option('l', "linesLength", HelpText = "Lines max length. 0 - not change, -1 - merge lines")]
        public int? LinesMaxLength { get; set; } = null;

        [Option('m', "linksMap", HelpText = "source -> replacement map for links (including images)")]
        public string? LinksMapFileName { get; set; } = null;

        [Option("outDir")]
        public string? OutputDirectory { get; set; } = null;

        [Option]
        public bool? RemoveTitleHeader { get; set; } = null;

        [Option]
        public bool CheckLinks { get; set; } = true;

        [Option]
        public bool RemoveSpoilers { get; set; } = false;

        [Option]
        public bool RemoveComments { get; set; } = false;
    }
}
