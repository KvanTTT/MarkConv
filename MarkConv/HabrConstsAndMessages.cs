namespace MarkConv
{
    public class HabrConstsAndMessages
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
    }
}