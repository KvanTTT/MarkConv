namespace HabraMark
{
    public class ProcessorOptions
    {
        /// <summary>
        /// 0 - not change
        /// -1 - concat lines
        /// </summary>
        public int LinesMaxLength { get; set; } = -1;

        public bool RemoveTitleHeader { get; set; } = false;

        public MarkdownType InputMarkdownType { get; set; } = MarkdownType.Default;

        public MarkdownType OutputMarkdownType { get; set; } = MarkdownType.Default;

        public string HeaderImageLink { get; set; } = string.Empty;

        public bool RemoveUnwantedBreaks { get; set; } = true;

        public bool Normalize { get; set; } = false;

        public static ProcessorOptions FromOptions(ProcessorOptions options)
        {
            return (ProcessorOptions)options.MemberwiseClone();
        }

        public static ProcessorOptions CreateGitHubToHabrahabrOptions()
        {
            return new ProcessorOptions
            {
                LinesMaxLength = -1,
                RemoveTitleHeader = true,
                InputMarkdownType = MarkdownType.GitHub,
                OutputMarkdownType = MarkdownType.Habrahabr,
            };
        }

        public static ProcessorOptions CreateHabrahabrToGitHubOptions()
        {
            return new ProcessorOptions
            {
                LinesMaxLength = 80,
                InputMarkdownType = MarkdownType.Habrahabr,
                OutputMarkdownType = MarkdownType.GitHub,
            };
        }

        public static ProcessorOptions CreateVisualCodeToGitHubOptions()
        {
            return new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.VisualCode,
                OutputMarkdownType = MarkdownType.GitHub,
            };
        }

        public static ProcessorOptions CreateGitHubToVisualCodeOptions()
        {
            return new ProcessorOptions
            {
                LinesMaxLength = 0,
                InputMarkdownType = MarkdownType.GitHub,
                OutputMarkdownType = MarkdownType.VisualCode,
            };
        }
    }
}
