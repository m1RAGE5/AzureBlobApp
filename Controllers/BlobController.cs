using AzureBlobApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureBlobApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlobController : ControllerBase
    {
        private readonly AzureBlobService _blobService;

        public BlobController(AzureBlobService blobService)
        {
            _blobService = blobService;
        }

        // POST api/blob/container/{containerName}
        [HttpPost("container/{containerName}")]
        public async Task<IActionResult> CreateContainer(string containerName)
        {
            var success = await _blobService.CreateContainerAsync(containerName);
            if (success) return Ok($"Контейнер '{containerName}' успішно створено.");
            return BadRequest("Помилка при створенні контейнера.");
        }

        // DELETE api/blob/container/{containerName}
        [HttpDelete("container/{containerName}")]
        public async Task<IActionResult> DeleteContainer(string containerName)
        {
            var success = await _blobService.DeleteContainerAsync(containerName);
            if (success) return Ok($"Контейнер '{containerName}' успішно видалено.");
            return NotFound($"Контейнер '{containerName}' не знайдено.");
        }

        // POST api/blob/{containerName}/upload
        [HttpPost("{containerName}/upload")]
        public async Task<IActionResult> UploadBlob(string containerName, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не вибрано.");

            using var stream = file.OpenReadStream();
            var uri = await _blobService.UploadBlobAsync(containerName, file.FileName, stream);
            return Ok(new { Message = "Файл успішно завантажено.", Uri = uri });
        }

        // GET api/blob/{containerName}/list
        [HttpGet("{containerName}/list")]
        public async Task<IActionResult> ListBlobs(string containerName)
        {
            var blobs = await _blobService.ListBlobsAsync(containerName);
            return Ok(blobs);
        }

        // DELETE api/blob/{containerName}/{blobName}
        [HttpDelete("{containerName}/{blobName}")]
        public async Task<IActionResult> DeleteBlob(string containerName, string blobName)
        {
            var success = await _blobService.DeleteBlobAsync(containerName, blobName);
            if (success) return Ok($"Файл '{blobName}' успішно видалено.");
            return NotFound($"Файл '{blobName}' не знайдено.");
        }
    }
}