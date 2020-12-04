using System.Text.RegularExpressions;

namespace MarkConv
{
    public class Postprocessor
    {
        public const int HabrMaxTextLengthWithoutCut = 1000;
        public const int HabrMaxTextLengthBeforeCut = 2000;
        public const int HabrMinTextLengthBeforeCut = 100;
        public const int HabrMinTextLengthAfterCut = 100;

        public static readonly string HabrMaxTextLengthWithoutCutMessage =
            $"You need to insert <cut/> tag if the text contains more than {HabrMaxTextLengthWithoutCut} characters";
        public static readonly string HabrMaxTextLengthBeforeCutMessage =
            $"Text before cut can not be more than or equal to {HabrMaxTextLengthBeforeCut} characters";
        public static readonly string HabrMinTextLengthBeforeCutMessage =
            $"Text before cut can not be less than {HabrMinTextLengthBeforeCut} characters";
        public static readonly string HabrMinTextLengthAfterCutMessage =
            $"Text after cut can not be less than {HabrMinTextLengthAfterCut} characters";

        private static readonly Regex CutTagRegex = new Regex(@"<(habra)?cut\s*(text\s*=\s*""(.*?)""\s*)?/?>");

        private readonly ProcessorOptions _options;
        private readonly ILogger _logger;

        public Postprocessor(ProcessorOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        public void Postprocess(string result)
        {
            var cutMatch = CutTagRegex.Match(result);

            if (_options.OutputMarkdownType == MarkdownType.Habr)
            {
                if (!cutMatch.Success)
                {
                    if (result.Trim().Length >= HabrMaxTextLengthWithoutCut)
                    {
                        _logger.Error(HabrMaxTextLengthWithoutCutMessage);
                    }
                }
                else
                {
                    int cutElementIndex = cutMatch.Index;
                    if (cutElementIndex > HabrMaxTextLengthBeforeCut)
                    {
                        _logger.Error(HabrMaxTextLengthBeforeCutMessage);
                    }
                    else if (cutElementIndex < HabrMinTextLengthBeforeCut)
                    {
                        _logger.Error(HabrMinTextLengthBeforeCutMessage);
                    }

                    if (result.Length - (cutElementIndex + 4) < HabrMinTextLengthAfterCut) // TODO: Bug on habr.com
                    {
                        _logger.Error(HabrMinTextLengthAfterCutMessage);
                    }
                }
            }
        }
    }
}