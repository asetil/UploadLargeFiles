using UploadingLargeFiles.DTO;

namespace UploadingLargeFiles.Services
{
    public interface IFileService
    {
        Task<FileUploadSummary> UploadFileAsync(Stream fileStream, string contentType);
        Task UploadFileChunkAsync(IFormFile file, string fileName, int chunkIndex);
        Task MergeChunks(string fileName);
    }
}
