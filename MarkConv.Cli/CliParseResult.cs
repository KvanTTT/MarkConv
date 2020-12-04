namespace MarkConv.Cli
{
    public class CliParseResult<TParameters>
        where TParameters : new()
    {
        public TParameters Parameters { get; }

        public bool ShowHelp { get; }

        public bool ShowVersion { get; }

        public CliParseResult(TParameters parameters, bool showHelp, bool showVersion)
        {
            Parameters = parameters;
            ShowHelp = showHelp;
            ShowVersion = showVersion;
        }
    }
}
