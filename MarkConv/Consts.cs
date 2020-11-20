using System.Text.RegularExpressions;

namespace MarkConv
{
    public class Consts
    {
        public static readonly char[] SpaceChars = { ' ', '\t' };
        public static readonly Regex SpecialCharsRegex = new Regex($@"^(>|\*|-|\+|\d+\.|\||=)$", RegexOptions.Compiled);
    }
}