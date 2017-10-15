using System;
using System.Collections.Generic;
using System.IO;

namespace MarkConv
{
    public class ImagesMap
    {
        public static Dictionary<string, ImageHash> Load(string imagesMapFileName, string rootDir, ILogger logger)
        {
            var imagesMap = new Dictionary<string, ImageHash>();
            if (string.IsNullOrEmpty(imagesMapFileName))
                return imagesMap;

            string[] mappingItems = File.ReadAllLines(imagesMapFileName);
            for (int i = 0; i < mappingItems.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(mappingItems[i]) || mappingItems[i].TrimStart().StartsWith("//"))
                    continue;

                string[] strs = mappingItems[i].Split(MarkdownRegex.SpaceChars, StringSplitOptions.RemoveEmptyEntries);
                if (strs.Length != 2)
                {
                    logger?.Warn($"Incorrect mapping item \"{mappingItems[i]}\" at line {i + 1}");
                }
                else
                {
                    string source = strs[0];
                    string replacement = strs[1];

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
