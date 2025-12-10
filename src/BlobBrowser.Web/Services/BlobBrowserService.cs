using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobBrowser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BlobBrowser.Services
{
    public class BlobBrowserService : IBlobBrowserService
    {
        // Extracts base uri without query and sas query
        private static void SplitContainerUri(string containerSasUrl, out Uri baseNoQuery, out string sasQuery)
        {
            var uri = new Uri(containerSasUrl);
            var s = uri.ToString();
            var idx = s.IndexOf('?');
            if (idx >= 0)
            {
                baseNoQuery = new Uri(s.Substring(0, idx));
                sasQuery = s.Substring(idx); // includes '?'
            }
            else
            {
                baseNoQuery = uri;
                sasQuery = "";
            }
        }

        private static string BuildBlobUrl(Uri baseNoQuery, string sasQuery, string blobName)
        {
            var baseStr = baseNoQuery.ToString().TrimEnd('/');
            var escaped = string.Join("/", blobName.Split('/').Select(seg => Uri.EscapeDataString(seg)));
            return $"{baseStr}/{escaped}{sasQuery}";
        }

        public async Task<BlobListResponse> ListDirectoryAsync(string containerSasUrl, string? path, string? continuationToken, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(containerSasUrl)) throw new ArgumentNullException(nameof(containerSasUrl));

            SplitContainerUri(containerSasUrl, out var baseNoQuery, out var sasQuery);

            var containerClient = new BlobContainerClient(new Uri(containerSasUrl));

            // Normalize prefix
            var prefix = string.IsNullOrEmpty(path) ? "" : path.TrimStart('/');
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/")) prefix += "/";

            var result = new BlobListResponse();

            string delimiter = "/";

            // Use GetBlobsByHierarchyAsync with AsPages for pagination
            var pages = containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: delimiter)
                                       .AsPages(continuationToken, pageSize);

            // Get first page only (server-side pagination)
            await foreach (var page in pages)
            {
                // directories (prefixes)
                foreach (var prefixItem in page.Values.Where(v => v.IsPrefix))
                {
                    var dirName = prefixItem.Prefix; // includes trailing '/'
                    var displayName = dirName.Substring(prefix.Length).TrimEnd('/');
                    result.Items.Add(new BlobItemViewModel
                    {
                        Name = displayName,
                        IsDirectory = true,
                        Path = dirName,
                        Url = $"/?path={HttpUtility.UrlEncode(dirName)}" // local app link to list that directory
                    });
                }

                // blobs
                foreach (var blobItem in page.Values.Where(v => !v.IsPrefix).Select(v => v.Blob))
                {
                    var blobName = blobItem.Name;
                    var displayName = blobName.Substring(prefix.Length);
                    result.Items.Add(new BlobItemViewModel
                    {
                        Name = displayName,
                        IsDirectory = false,
                        Size = blobItem.Properties.ContentLength,
                        LastModified = blobItem.Properties.LastModified,
                        Path = blobName,
                        Url = BuildBlobUrl(baseNoQuery, sasQuery, blobName)
                    });
                }

                // continuation token for next page
                result.ContinuationToken = page.ContinuationToken;
                break; // only the single page requested
            }

            // If zero pages returned, ContinuationToken remains null

            // Build breadcrumbs
            result.Breadcrumbs = BuildBreadcrumbs(prefix);

            return result;
        }

        public async Task<BlobListResponse> SearchAsync(string containerSasUrl, string searchTerm, string? continuationToken, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(containerSasUrl)) throw new ArgumentNullException(nameof(containerSasUrl));
            if (string.IsNullOrWhiteSpace(searchTerm)) throw new ArgumentNullException(nameof(searchTerm));

            SplitContainerUri(containerSasUrl, out var baseNoQuery, out var sasQuery);
            var containerClient = new BlobContainerClient(new Uri(containerSasUrl));

            var result = new BlobListResponse();

            // We'll iterate Azure pages until we've collected pageSize matched items or no more pages.
            string azureContinuation = continuationToken;
            // We set azurePageSize larger to reduce number of roundtrips while scanning; azurePageSize can be tuned.
            const int azurePageSize = 500;

            var pages = containerClient.GetBlobsAsync().AsPages(azureContinuation, azurePageSize);

            await foreach (var page in pages)
            {
                foreach (var blob in page.Values)
                {
                    if (blob.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Items.Add(new BlobItemViewModel
                        {
                            Name = blob.Name.Split('/').Last(),
                            IsDirectory = false,
                            Size = blob.Properties.ContentLength,
                            LastModified = blob.Properties.LastModified,
                            Path = blob.Name,
                            Url = BuildBlobUrl(baseNoQuery, sasQuery, blob.Name)
                        });

                        if (result.Items.Count >= pageSize)
                            break;
                    }
                }

                // If we've gathered enough matches, set continuation token and break
                result.ContinuationToken = page.ContinuationToken;
                if (result.Items.Count >= pageSize)
                    break;

                // else continue to next page
            }

            // For search, breadcrumbs are root-only (makes sense) — client may choose to ignore breadcrumbs.
            result.Breadcrumbs = new List<Breadcrumb> { new Breadcrumb("root", "", "/") };

            return result;
        }

        private static List<Breadcrumb> BuildBreadcrumbs(string prefix)
        {
            var breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("root", "", "/")
            };

            if (string.IsNullOrEmpty(prefix)) return breadcrumbs;

            var parts = prefix.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string accum = "";
            for (int i = 0; i < parts.Length; i++)
            {
                accum = string.IsNullOrEmpty(accum) ? parts[i] + "/" : accum + parts[i] + "/";
                breadcrumbs.Add(new Breadcrumb(parts[i], accum, $"/?path={HttpUtility.UrlEncode(accum)}"));
            }

            return breadcrumbs;
        }
    }
}
