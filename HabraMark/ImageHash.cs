namespace HabraMark
{
    public class ImageHash
    {
        public string Path { get; set; }

        public byte[] Hash { get; set; }

        public ImageHash(string path, byte[] hash)
        {
            Path = path;
            Hash = hash;
        }

        public override string ToString() => Path;
    }
}
