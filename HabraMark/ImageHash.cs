using System;

namespace HabraMark
{
    public class ImageHash
    {
        public string Path { get; set; }

        public string RootDir { get; set; }

        public Lazy<byte[]> Hash => new Lazy<byte[]>(() => Link.GetImageHash(Path, RootDir));

        public ImageHash(string path, string rootDir)
        {
            Path = path;
            RootDir = rootDir;
        }

        public override string ToString() => Path;
    }
}
