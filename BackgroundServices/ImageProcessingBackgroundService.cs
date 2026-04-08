using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace AzureBlobApp.BackgroundServices
{
    public class ImageProcessingBackgroundService : BackgroundService
    {
        private readonly ILogger<ImageProcessingBackgroundService> _logger;
        private readonly QueueClient _queueClient;
        private readonly BlobContainerClient _bigImagesContainer;
        private readonly BlobContainerClient _smallImagesContainer;

        public ImageProcessingBackgroundService(ILogger<ImageProcessingBackgroundService> logger)
        {
            _logger = logger;
            string connectionString = "UseDevelopmentStorage=true";

            _queueClient = new QueueClient(connectionString, "new-images");
            
            var blobServiceClient = new BlobServiceClient(connectionString);
            _bigImagesContainer = blobServiceClient.GetBlobContainerClient("big-images");
            _smallImagesContainer = blobServiceClient.GetBlobContainerClient("small-images");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background image processing service started.");

            await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
            await _smallImagesContainer.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 1, visibilityTimeout: TimeSpan.FromSeconds(30), cancellationToken: stoppingToken);

                    if (messages.Length > 0)
                    {
                        var message = messages[0];
                        
                        byte[] bytes = Convert.FromBase64String(message.Body.ToString());
                        string fileName = Encoding.UTF8.GetString(bytes);

                        _logger.LogInformation($"[START] New task. Start compression: {fileName}");

                        BlobClient originalBlob = _bigImagesContainer.GetBlobClient(fileName);
                        using var originalStream = new MemoryStream();
                        await originalBlob.DownloadToAsync(originalStream, cancellationToken: stoppingToken);
                        originalStream.Position = 0;

                        using var compressedStream = new MemoryStream();
                        using (Image image = await Image.LoadAsync(originalStream, cancellationToken: stoppingToken))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(200, 0),
                                Mode = ResizeMode.Max
                            }));

                            await image.SaveAsJpegAsync(compressedStream, cancellationToken: stoppingToken);
                        }
                        compressedStream.Position = 0;

                        BlobClient smallBlob = _smallImagesContainer.GetBlobClient(fileName);
                        await smallBlob.UploadAsync(compressedStream, overwrite: true, cancellationToken: stoppingToken);

                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: stoppingToken);

                        _logger.LogInformation($"[END] {fileName} successfully compressed and saved!");
                    }
                    else
                    {
                        await Task.Delay(2000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Background service error: {ex.Message}");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}