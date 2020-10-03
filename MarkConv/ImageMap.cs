using System;
using System.Collections.Generic;
using System.IO;

namespace MarkConv
{
    public static class ImagesMap
    {
        public const string DefaultImagesMapFileName = "ImagesMap";
        public const string HeaderImageLinkSrc = "HeaderImageLink";

        public static Dictionary<string, ImageHash> Load(string imagesMapFileName, string rootDir, ILogger logger)
        {
            var imagesMap = new Dictionary<string, ImageHash>();
            if (string.IsNullOrEmpty(imagesMapFileName))
            {
                string defaultImagesMapFile = Path.Combine(rootDir, DefaultImagesMapFileName);
                if (File.Exists(defaultImagesMapFile))
                {
                    imagesMapFileName = defaultImagesMapFile;
                }
                else
                {
                    return imagesMap;
                }
            }

            string[] mappingItems = File.ReadAllLines(imagesMapFileName);
            for (int i = 0; i < mappingItems.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(mappingItems[i]) || mappingItems[i].TrimStart().StartsWith("//"))
                    continue;

                string[] parts = mappingItems[i].Split(MarkdownRegex.SpaceChars, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    logger?.Warn($"Incorrect mapping item \"{mappingItems[i]}\" at line {i + 1}");
                }
                else
                {
                    string source = parts[0];
                    string replacement = parts[1];

                    if (imagesMap.ContainsKey(source))
                    {
                        logger?.Warn($"Duplicated {source} image at line {i + 1}");
                    }

                    imagesMap[source] = new ImageHash(replacement, rootDir);
                }
            }
            return imagesMap;
        }
    }
}
