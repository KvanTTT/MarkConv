using System;
using System.Collections.Generic;
using System.IO;

namespace MarkConv
{
    public static class LinksMap
    {
        private static readonly char[] SpaceChars = { ' ', '\t' };
        public const string DefaultImagesMapFileName = "ImagesMap";
        public const string HeaderImageLinkSrc = "HeaderImageLink";

        public static Dictionary<string, string> Load(string? linksMapFileName, string rootDir, ILogger logger)
        {
            var linksMap = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(linksMapFileName))
            {
                string defaultImagesMapFile = Path.Combine(rootDir, DefaultImagesMapFileName);
                if (File.Exists(defaultImagesMapFile))
                {
                    linksMapFileName = defaultImagesMapFile;
                }
                else
                {
                    return linksMap;
                }
            }

            string[] mappingItems = File.ReadAllLines(linksMapFileName);
            for (int i = 0; i < mappingItems.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(mappingItems[i]) || mappingItems[i].TrimStart().StartsWith("//"))
                    continue;

                string[] parts = mappingItems[i].Split(SpaceChars, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    logger?.Warn($"Incorrect mapping item \"{mappingItems[i]}\" at line {i + 1}");
                }
                else
                {
                    string source = parts[0];
                    string replacement = parts[1];

                    if (linksMap.ContainsKey(source))
                    {
                        logger?.Warn($"Duplicated {source} image at line {i + 1}");
                    }

                    linksMap[source] = replacement;
                }
            }

            return linksMap;
        }
    }
}
