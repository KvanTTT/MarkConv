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

        public RelativeLinksKind InputRelativeLinksKind { get; set; } = RelativeLinksKind.Default;

        public RelativeLinksKind OutputRelativeLinksKind { get; set; } = RelativeLinksKind.Default;

        public string HeaderImageLink { get; set; } = string.Empty;

        public bool ReplaceSpoilers { get; set; } = true;

        public bool RemoveUnwantedBreaks { get; set; } = true;

        public bool Normalize { get; set; } = false;

        public static ProcessorOptions FromOptions(ProcessorOptions options)
        {
            return (ProcessorOptions)options.MemberwiseClone();
        }
    }
}
