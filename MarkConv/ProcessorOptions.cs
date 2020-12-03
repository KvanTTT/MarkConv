using System.Collections.Generic;

namespace MarkConv
{
    public class ProcessorOptions
    {
        /// <summary>
        /// 0 - not change
        /// -1 - concat lines
        /// </summary>
        public int LinesMaxLength { get; set; }

        public MarkdownType InputMarkdownType { get; set; } = MarkdownType.GitHub;

        public MarkdownType OutputMarkdownType { get; set; } = MarkdownType.GitHub;

        public string HeaderImageLink { get; set; } = string.Empty;

        public bool RemoveTitleHeader { get; set; }

        public string IndentString { get; set; } = "    ";

        public bool CheckLinks { get; set; } = true;

        public bool CenterImageAlignment { get; set; }

        public string RootDirectory { get; set; } = "";

        public Dictionary<string, string> LinksMap = new Dictionary<string, string>();

        public bool RemoveDetails { get; set; }

        public bool RemoveComments { get; set; }

        public static ProcessorOptions FromOptions(ProcessorOptions options)
        {
            return (ProcessorOptions)options.MemberwiseClone();
        }

        public static ProcessorOptions GetDefaultOptions(MarkdownType inputMarkdownType, MarkdownType outputMarkdownType)
        {
            var options = new ProcessorOptions
            {
                InputMarkdownType = inputMarkdownType,
                OutputMarkdownType = outputMarkdownType
            };
            switch (inputMarkdownType)
            {
                case MarkdownType.Habr:
                case MarkdownType.Dev:
                    options.LinesMaxLength = outputMarkdownType == MarkdownType.GitHub ? 80 : 0;
                    break;
                case MarkdownType.GitHub:
                    options.LinesMaxLength =
                        outputMarkdownType == MarkdownType.Habr || outputMarkdownType == MarkdownType.Dev ? -1 : 0;
                    break;
            }
            if (outputMarkdownType == MarkdownType.Habr || outputMarkdownType == MarkdownType.Dev)
            {
                options.IndentString = "    ";
                options.RemoveTitleHeader = true;
                options.CenterImageAlignment = true;
            }
            return options;
        }
    }
}
