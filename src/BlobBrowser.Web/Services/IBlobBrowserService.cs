using BlobBrowser.Models;
using System.Threading.Tasks;

namespace BlobBrowser.Services
{
    public interface IBlobBrowserService
    {
        Task<BlobListResponse> ListDirectoryAsync(string containerSasUrl, string? path, string? continuationToken, int pageSize);
        Task<BlobListResponse> SearchAsync(string containerSasUrl, string searchTerm, string? continuationToken, int pageSize);
    }
}
