using System;
using System.Collections.Generic;
using System.IO;

namespace HabraMark
{
    public class ImagesMap
    {
        public static Dictionary<string, ImageHash> Load(string imagesMapFileName, bool checkLinks, ILogger logger, string rootDir)
        {
            var imagesMap = new Dictionary<string, ImageHash>();
            if (string.IsNullOrEmpty(imagesMapFileName))
                return imagesMap;

            string[] mappingItems = File.ReadAllLines(imagesMapFileName);
            for (int i = 0; i < mappingItems.Length; i++)
            {
                string[] strs = mappingItems[i].Split(MarkdownRegex.SpaceChars, StringSplitOptions.RemoveEmptyEntries);
                if (strs.Length != 2)
                {
                    logger?.Warn($"Incorrect mapping item {mappingItems[i]} at line {i + 1}");
                }
                else
                {
                    string source = strs[0];
                    string replacement = strs[1];

                    if (imagesMap.ContainsKey(source))
                    {
                        logger?.Warn($"duplicated {source} image ar line {i + 1}");
                    }

                    byte[] hash1 = null, hash2 = null;
                    if (checkLinks)
                    {
                        hash1 = Link.GetImageHash(source, rootDir);
                        hash2 = Link.GetImageHash(replacement, rootDir);
                        if (hash1 != null && hash2 != null && !Link.CompareHashes(hash1, hash2))
                        {
                            logger?.Warn($"{source} or {replacement} address is incorrect or images are different");
                        }
                    }
                    imagesMap[source] = new ImageHash(replacement, hash1 ?? hash2);
                }
            }
            return imagesMap;
        }
    }
}
