using System;
using System.Collections.Generic;
using System.Text;
using MarkConv.Nodes;
using Markdig;
using Markdig.Syntax;

namespace MarkConv
{
    public class HeaderToLinkConverter
    {
        private readonly Dictionary<string, Anchor> _anchors;

        public HeaderToLinkConverter(Dictionary<string, Anchor> anchors)
        {
            _anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
        }

        public string Convert(Node resultAnchorNode, MarkdownType markdownType)
        {
            string title;
            if (markdownType == MarkdownType.Habr)
            {
                char headingChar = '#';

                if (resultAnchorNode is MarkdownNode markdownNode &&
                    markdownNode.MarkdownObject is HeadingBlock headingBlock)
                {
                    headingChar = headingBlock.HeaderChar;
                }

                title = resultAnchorNode.Substring.Trim(headingChar, ' ');
            }
            else
            {
                title = Markdown.ToPlainText(resultAnchorNode.Substring).Trim();
            }

            return Convert(title, resultAnchorNode, markdownType);
        }

        public string Convert(string address, MarkdownType markdownType)
        {
            return Convert(address, null, markdownType);
        }

        private string Convert(string title, Node resultAnchorNode, MarkdownType markdownType)
        {
            string address = ConvertHeaderTitleToLink(title, markdownType);

            int newNumber = 0;
            string newAddress = address;

            if (_anchors.TryGetValue(address, out Anchor foundAnchor))
            {
                do
                {
                    if (title != foundAnchor.Title || foundAnchor.Number == 0)
                    {
                        newAddress = $"{newAddress}-1";
                        newNumber = 1;
                    }
                    else
                    {
                        var number = foundAnchor.Number;
                        newNumber = number + 1;
                        newAddress = $"{newAddress.Remove(newAddress.Length - number.ToString().Length)}{newNumber}";
                    }
                } while (_anchors.TryGetValue(newAddress, out foundAnchor));
            }

            if (resultAnchorNode != null)
            {
                var result = new Anchor(resultAnchorNode, title, newAddress, newNumber);
                _anchors.Add(newAddress, result);
            }

            return newAddress;
        }

        public static string ConvertHeaderTitleToLink(string title, MarkdownType markdownType)
        {
            var result = new StringBuilder(title.Length);

            foreach (char c in title)
            {
                var lower = char.ToLowerInvariant(c);
                if (markdownType == MarkdownType.Habr)
                {
                    if (lower >= 'a' && lower <= 'z' || lower >= '0' && lower <= '9')
                        result.Append(lower);
                    else if (Consts.RussianTransliterationMap.TryGetValue(lower, out string replacement))
                        result.Append(replacement);
                }
                else
                {
                    if (char.IsLetterOrDigit(lower))
                    {
                        result.Append(lower);
                    }
                    else
                    {
                        if (lower == ' ' || lower == '-')
                            result.Append('-');
                        else if (lower == '_')
                            result.Append('_');
                    }
                }
            }

            return result.ToString();
        }
    }
}