using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureBlobApp.Services
{
    public class AzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<bool> CreateContainerAsync(string containerName)
        {
            try
            {
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                return true;
            }
            catch (Exception ex)
      {
                Console.WriteLine($"[CONTAINER CREATION ERROR]: {ex.Message}\n");
                return false;
            }
        }

        public async Task<bool> DeleteContainerAsync(string containerName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            return await containerClient.DeleteIfExistsAsync();
        }

        public async Task<string> UploadBlobAsync(string containerName, string blobName, Stream content)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, overwrite: true);
            return blobClient.Uri.ToString();
        }

        public async Task<List<string>> ListBlobsAsync(string containerName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            List<string> blobNames = new List<string>();

            if (await containerClient.ExistsAsync())
            {
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    blobNames.Add(blobItem.Name);
                }
            }
            return blobNames;
        }

        public async Task<bool> DeleteBlobAsync(string containerName, string blobName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            return await blobClient.DeleteIfExistsAsync();
        }
    }
}