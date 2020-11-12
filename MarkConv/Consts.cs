using System.Collections.Generic;
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
        public static readonly Dictionary<char, string> RussianTransliterationMap = new Dictionary<char, string>
        {
            ['а'] = "a",
            ['б'] = "b",
            ['в'] = "v",
            ['г'] = "g",
            ['д'] = "d",
            ['е'] = "e",
            ['ё'] = "yo",
            ['ж'] = "zh",
            ['з'] = "z",
            ['и'] = "i",
            ['й'] = "y",
            ['к'] = "k",
            ['л'] = "l",
            ['м'] = "m",
            ['н'] = "n",
            ['о'] = "o",
            ['п'] = "p",
            ['р'] = "r",
            ['с'] = "s",
            ['т'] = "t",
            ['у'] = "u",
            ['ф'] = "f",
            ['х'] = "h",
            ['ц'] = "c",
            ['ч'] = "ch",
            ['ш'] = "sh",
            ['щ'] = "sch",
            ['ы'] = "y",
            ['э'] = "e",
            ['ю'] = "yu",
            ['я'] = "ya",
            ['-'] = "-",
            ['_'] = "_",
            [' '] = "-"
        };
    }
}