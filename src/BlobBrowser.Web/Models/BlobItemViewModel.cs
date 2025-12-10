namespace BlobBrowser.Models
{
    public class BlobItemViewModel
    {
        public string Name { get; set; } = "";
        public bool IsDirectory { get; set; }
        public long? Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public string Path { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
