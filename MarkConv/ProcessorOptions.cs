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

        public MarkdownType InputMarkdownType { get; set; } = MarkdownType.Default;

        public MarkdownType OutputMarkdownType { get; set; } = MarkdownType.Default;

        public string HeaderImageLink { get; set; } = string.Empty;

        public bool RemoveTitleHeader { get; set; }

        public bool NormalizeBreaks { get; set; } = true;

        public bool Normalize { get; set; }

        public string IndentString { get; set; } = "    ";

        public bool CheckLinks { get; set; } = true;

        public bool CompareImageHashes { get; set; }

        public bool CenterImageAlignment { get; set; }

        public string RootDirectory { get; set; } = "";

        public Dictionary<string, ImageHash> ImagesMap = new Dictionary<string, ImageHash>();

        public bool RemoveSpoilers { get; set; }

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
                    if (outputMarkdownType != MarkdownType.Habr)
                    {
                        options.LinesMaxLength = 80;
                    }
                    break;
                case MarkdownType.Common:
                    if (outputMarkdownType == MarkdownType.Habr)
                    {
                        options.LinesMaxLength = -1;
                    }
                    else
                    {
                        options.LinesMaxLength = 0;
                    }
                    break;
            }
            if (outputMarkdownType == MarkdownType.Habr)
            {
                options.IndentString = "    ";
                options.RemoveTitleHeader = true;
                options.CenterImageAlignment = true;
            }
            return options;
        }
    }
}
