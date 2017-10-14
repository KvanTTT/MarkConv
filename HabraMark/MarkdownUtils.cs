using System.Text.RegularExpressions;

namespace HabraMark
{
    public static class MarkdownUtils
    {
        public static string ExtractLinkTitle(this string text)
        {
            Match match = MarkdownRegex.LinkRegex.Match(text);
            if (match.Success)
            {
                return match.Groups[2].Value;
            }

            return text;
        }
    }
}
