using System.Text.RegularExpressions;

namespace MarkConv
{
    public class Consts
    {
        public static readonly char[] SpaceChars = { ' ', '\t' };
        public static readonly Regex SpecialCharsRegex = new Regex($@"^(>|\*|-|\+|\d+\.|\||=)$", RegexOptions.Compiled);
        public static readonly Regex UrlRegex = new Regex(
            @"https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,}",
            RegexOptions.Compiled);
    }
}