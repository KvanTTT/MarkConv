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

        public MarkdownType OutputMarkdownType { get; set; } = MarkdownType.Habr;

        public bool RemoveTitleHeader { get; set; }

        public string IndentString { get; set; } = "    ";

        public bool CheckLinks { get; set; }

        public bool CenterImageAlignment { get; set; }

        public string RootDirectory { get; set; } = "";

        public bool RemoveDetails { get; set; }

        public bool RemoveComments { get; set; }

        public static ProcessorOptions GetDefaultOptions(MarkdownType? inputMarkdownType, MarkdownType? outputMarkdownType)
        {
            var options = new ProcessorOptions();
            if (inputMarkdownType.HasValue)
                options.InputMarkdownType = inputMarkdownType.Value;

            if (outputMarkdownType.HasValue)
                options.OutputMarkdownType = outputMarkdownType.Value;

            switch (options.InputMarkdownType)
            {
                case MarkdownType.Habr:
                case MarkdownType.Dev:
                    options.LinesMaxLength = outputMarkdownType == MarkdownType.GitHub ? 80 : 0;
                    break;
                case MarkdownType.GitHub:
                    options.LinesMaxLength =
                        options.OutputMarkdownType == MarkdownType.Habr || outputMarkdownType == MarkdownType.Dev ? -1 : 0;
                    break;
            }
            if (options.OutputMarkdownType == MarkdownType.Habr || options.OutputMarkdownType == MarkdownType.Dev)
            {
                options.IndentString = "    ";
                options.RemoveTitleHeader = true;
                options.CenterImageAlignment = true;
            }
            return options;
        }
    }
}
