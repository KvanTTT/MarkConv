using System;

namespace MarkConv
{
    public class ImageHash
    {
        public string Path { get; }

        public string RootDir { get; }

        public Lazy<byte[]> Hash => new Lazy<byte[]>(() => Link.GetImageHash(Path, RootDir));

        public ImageHash(string path, string rootDir)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            RootDir = rootDir ?? throw new ArgumentNullException(nameof(rootDir));
        }

        public override string ToString() => Path;
    }
}
