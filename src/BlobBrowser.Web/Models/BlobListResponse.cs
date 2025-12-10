using System.Collections.Generic;

namespace BlobBrowser.Models
{
    public class BlobListResponse
    {
        public List<BlobItemViewModel> Items { get; set; } = new();
        public string? ContinuationToken { get; set; }
        public List<Breadcrumb> Breadcrumbs { get; set; } = new();
    }

    public record Breadcrumb(string Name, string Path, string Url);
}
