using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using System.Text;

namespace AzureBlobApp.Services
{
    public class AzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;

        public AzureBlobService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
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

        public async Task<string> UploadImageAsync(string fileName, Stream content)
        {
            try
            {
                string containerName = "big-images";
                string queueName = "new-images";

                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                BlobClient blobClient = containerClient.GetBlobClient(fileName);
                await blobClient.UploadAsync(content, overwrite: true);

                QueueClient queueClient = _queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();

                string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileName));
                await queueClient.SendMessageAsync(base64Message);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAGE PROCESSING ERROR]: {ex.Message}\n");
                throw;
            }
        }
    }
}