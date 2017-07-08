using System.Collections.Generic;

namespace HabraMark
{
    public class ProcessorOptions
    {
        /// <summary>
        /// 0 - not change
        /// -1 - concat lines
        /// </summary>
        public int LinesMaxLength { get; set; } = 0;

        public MarkdownType InputMarkdownType { get; set; } = MarkdownType.Default;

        public MarkdownType OutputMarkdownType { get; set; } = MarkdownType.Default;

        public string HeaderImageLink { get; set; } = string.Empty;

        public bool RemoveTitleHeader { get; set; } = false;

        public bool RemoveUnwantedBreaks { get; set; } = true;

        public bool Normalize { get; set; } = false;

        public string IndentString { get; set; } = "    ";

        public bool CheckLinks { get; set; } = true;

        public bool CompareImageHashes { get; set; } = false;

        public string RootDirectory { get; set; } = "";

        public Dictionary<string, ImageHash> ImagesMap = new Dictionary<string, ImageHash>();

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
                case MarkdownType.Habrahabr:
                    if (outputMarkdownType != MarkdownType.Habrahabr)
                    {
                        options.LinesMaxLength = 80;
                    }
                    options.RemoveTitleHeader = true;
                    break;
                case MarkdownType.GitHub:
                case MarkdownType.VisualCode:
                    if (outputMarkdownType == MarkdownType.Habrahabr)
                    {
                        options.LinesMaxLength = -1;
                    }
                    else
                    {
                        options.LinesMaxLength = 0;
                    }
                    break;
                default:
                case MarkdownType.Default:
                    break;
            }
            return options;
        }
    }
}
