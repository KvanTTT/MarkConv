using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace HabraMark
{
    public class Link
    {
        public string Title { get; set; }

        public string Address { get; set; }

        public bool IsImage { get; set; }

        public LinkType LinkType { get; set; }

        public Link(string title, string address, bool isImage = false, LinkType linkType = LinkType.Absolute)
        {
            Title = title;
            Address = address;
            IsImage = isImage;
            LinkType = linkType;
        }

        public override string ToString()
        {
            return $"{(IsImage ? "!" : "")}[{Title}]({(LinkType == LinkType.Relative ? "#" : "")}{Address})";
        }

        public static bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1 == null || hash2 == null)
                return false;

            if (hash1.Length != hash2.Length)
                return false;

            for (int i = 0; i < hash1.Length; i++)
                if (hash1[i] != hash2[i])
                    return false;

            return true;
        }

        public static byte[] GetImageHash(string pathOrUrl, string rootDir)
        {
            LinkType linkType = DetectLinkType(pathOrUrl);
            byte[] data = null;
            if (linkType == LinkType.Local)
            {
                try
                {
                    data = File.ReadAllBytes(Path.Combine(rootDir, pathOrUrl));
                }
                catch
                {
                }
            }
            else if (linkType == LinkType.Absolute)
            {
                data = DownloadData(pathOrUrl);
            }

            if (data != null)
            {
                byte[] hash = null;
                using (SHA1 sha1 = SHA1.Create())
                {
                    hash = sha1.ComputeHash(data);
                }
                return hash;
            }
            return null;
        }

        public static LinkType DetectLinkType(string address)
        {
            address = address.Trim();
            if (MarkdownRegex.UrlRegex.IsMatch(address))
                return LinkType.Absolute;

            if (address.StartsWith("#"))
                return LinkType.Relative;

            return LinkType.Local;
        }

        public static bool IsUrlValid(string url, int timeout = 5000) =>
            DownloadData(url, timeout) != null;

        public static byte[] DownloadData(string url, int timeout = 5000)
        {
            HttpClient client = new HttpClient();
            byte[] data = null;
            try
            {
                client.Timeout = TimeSpan.FromMilliseconds(timeout);
                data = client.GetByteArrayAsync(url).Result;
            }
            catch
            {
            }
            finally
            {
                client.Dispose();
            }
            return data;
        }
    }
}
