using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;

namespace AdminMembers.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly IConfiguration _configuration;

        public BlobStorageService(BlobServiceClient blobServiceClient, ILogger<BlobStorageService> logger, IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Uploads a blob to the specified container
        /// </summary>
        public async Task<string> UploadBlobAsync(string containerName, string blobName, Stream content, string contentType = "application/octet-stream")
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(blobName);

                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                };

                await blobClient.UploadAsync(content, uploadOptions, cancellationToken: default);

                _logger.LogInformation($"Uploaded blob {blobName} to container {containerName}");

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading blob {blobName} to container {containerName}");
                throw;
            }
        }

        /// <summary>
        /// Uploads a blob from byte array to the specified container
        /// </summary>
        public async Task<string> UploadBlobAsync(string containerName, string blobName, byte[] content, string contentType = "application/octet-stream")
        {
            using var stream = new MemoryStream(content);
            return await UploadBlobAsync(containerName, blobName, stream, contentType);
        }

        /// <summary>
        /// Downloads a blob from the specified container
        /// </summary>
        public async Task<byte[]> DownloadBlobAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                {
                    _logger.LogWarning($"Blob {blobName} not found in container {containerName}");
                    throw new FileNotFoundException($"Blob {blobName} not found in container {containerName}");
                }

                var downloadResult = await blobClient.DownloadContentAsync();

                _logger.LogInformation($"Downloaded blob {blobName} from container {containerName}");

                return downloadResult.Value.Content.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading blob {blobName} from container {containerName}");
                throw;
            }
        }

        /// <summary>
        /// Downloads a blob to a stream from the specified container
        /// </summary>
        public async Task DownloadBlobToStreamAsync(string containerName, string blobName, Stream destination)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                {
                    _logger.LogWarning($"Blob {blobName} not found in container {containerName}");
                    throw new FileNotFoundException($"Blob {blobName} not found in container {containerName}");
                }

                await blobClient.DownloadToAsync(destination);

                _logger.LogInformation($"Downloaded blob {blobName} from container {containerName} to stream");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading blob {blobName} from container {containerName} to stream");
                throw;
            }
        }

        /// <summary>
        /// Lists all blobs in the specified container with optional prefix filter
        /// </summary>
        public async Task<List<BlobInfo>> ListBlobsAsync(string containerName, string? prefix = null)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                var exists = await containerClient.ExistsAsync();
                if (!exists.Value)
                {
                    _logger.LogWarning($"Container {containerName} not found");
                    return new List<BlobInfo>();
                }

                var blobs = new List<BlobInfo>();

                await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
                {
                    blobs.Add(new BlobInfo
                    {
                        Name = blobItem.Name,
                        ContentType = blobItem.Properties.ContentType,
                        ContentLength = blobItem.Properties.ContentLength ?? 0,
                        CreatedOn = blobItem.Properties.CreatedOn ?? DateTimeOffset.MinValue,
                        LastModified = blobItem.Properties.LastModified ?? DateTimeOffset.MinValue
                    });
                }

                _logger.LogInformation($"Listed {blobs.Count} blobs in container {containerName}");

                return blobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing blobs in container {containerName}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a blob from the specified container
        /// </summary>
        public async Task<bool> DeleteBlobAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var result = await blobClient.DeleteIfExistsAsync();

                if (result.Value)
                {
                    _logger.LogInformation($"Deleted blob {blobName} from container {containerName}");
                }
                else
                {
                    _logger.LogWarning($"Blob {blobName} not found in container {containerName}");
                }

                return result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blob {blobName} from container {containerName}");
                throw;
            }
        }

        /// <summary>
        /// Gets the URI of a blob
        /// </summary>
        public async Task<string> GetBlobUriAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                {
                    throw new FileNotFoundException($"Blob {blobName} not found in container {containerName}");
                }

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting URI for blob {blobName} in container {containerName}");
                throw;
            }
        }

        /// <summary>
        /// Checks if a blob exists in the specified container
        /// </summary>
        public async Task<bool> BlobExistsAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var exists = await blobClient.ExistsAsync();
                return exists.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if blob {blobName} exists in container {containerName}");
                return false;
            }
        }
    }

    public class BlobInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long ContentLength { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }
}
