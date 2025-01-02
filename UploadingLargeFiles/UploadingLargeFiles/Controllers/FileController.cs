using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using UploadingLargeFiles.Services;
using UploadingLargeFiles.Utilities;

namespace UploadingLargeFiles.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload-stream-multipartreader")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [MultipartFormData]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> Upload()
        {
            var fileUploadSummary = await _fileService.UploadFileAsync(HttpContext.Request.Body, Request.ContentType);

            return CreatedAtAction(nameof(Upload), fileUploadSummary);
        }

        [HttpPost("upload-chunk")]
        public async Task<IActionResult> UploadChunk([FromForm] IFormFile chunk, [FromForm] int chunkNumber, [FromForm] int totalChunks, [FromForm] string fileName)
        {
            if (chunk == null || chunk.Length == 0)
            {
                return BadRequest("No chunk received.");
            }

            await _fileService.UploadFileChunkAsync(chunk, fileName, chunkNumber);

            if (chunkNumber == totalChunks)
            {
                await _fileService.MergeChunks(fileName);
            }

            //var uploadPath = Path.Combine("uploads", fileName);
            //await using var stream = new FileStream(uploadPath, FileMode.Append, FileAccess.Write);

            //await chunk.CopyToAsync(stream);

            //if (chunkNumber == totalChunks)
            //{
            //    // Tüm parçalar tamamlandıktan sonra işlem yapabilirsiniz.
            //    return Ok("File upload complete");
            //}

            return Ok("Chunk received");
        }
    }

    public class FileUploadRequest
    {
        public IFormFile? Chunk { get; set; }
        public int ChunkNumber { get; set; }
        public int TotalChunks { get; set; }
        public string FileName { get; set; };
    }
}