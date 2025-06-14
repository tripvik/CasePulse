using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace SmartPendant.MAUIHybrid.Services
{
    public interface IStorageService
    {
        public Task<string> UploadAudioAsync(Stream audioStream, string fileName);
    }

    public class BlobStorageService : IStorageService
    {
        private string connectionString;
        private string containerName;
        private readonly IConfiguration _configuration;

        public BlobStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetValue<string>("Azure:BlobStorage:ConnectionString") ?? throw new InvalidOperationException("BlobStorage:ConnectionString Please check your appsettings.json or environment variables.");
            containerName = _configuration.GetValue<string>("Azure:BlobStorage:ContainerName") ?? throw new InvalidOperationException("BlobStorage:ContainerName is not configured. Please check your appsettings.json or environment variables.");
        }
        public async Task<string> UploadAudioAsync(Stream audioStream, string fileName)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(audioStream, overwrite: true);
            return blobClient.Uri.ToString();
        }
    }
}
