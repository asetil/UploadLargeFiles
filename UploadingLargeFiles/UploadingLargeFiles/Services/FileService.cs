using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using UploadingLargeFiles.DTO;

namespace UploadingLargeFiles.Services
{
    public class FileService : IFileService
    {

        private readonly IEnumerable<string> allowedExtensions = new List<string> { ".zip", ".bin", ".png", ".jpg", ".pdf" };

        public async Task<FileUploadSummary> UploadFileAsync(Stream fileStream, string contentType)
        {
            var fileCount = 0;
            long totalSizeInBytes = 0;

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType));
            var multipartReader = new MultipartReader(boundary, fileStream);
            var section = await multipartReader.ReadNextSectionAsync();

            var filePaths = new List<string>();
            var notUploadedFiles = new List<string>();
            var saveTasks = new List<Task<long>>();

            var direcory = $"FilesUploaded/{Guid.NewGuid().ToString()}";
            Directory.CreateDirectory(direcory);

            //int chunkNumber = 0;
            //string fileName = null;

            while (section != null)
            {
                //var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                //if (hasContentDispositionHeader)
                //{
                //    if (contentDisposition.DispositionType.Equals("form-data"))
                //    {
                //        if (contentDisposition.Name == "chunkNumber")
                //        {
                //            Chunk numarasını al
                //            using var reader = new StreamReader(section.Body);
                //            chunkNumber = int.Parse(await reader.ReadToEndAsync());
                //        }
                //        else if (contentDisposition.Name == "fileName")
                //        {
                //            Dosya adını al
                //            using var reader = new StreamReader(section.Body);
                //            fileName = await reader.ReadToEndAsync();
                //        }
                //        else if (contentDisposition.FileName.HasValue)
                //        {
                //            Chunk içeriğini dosyaya yaz
                //            var uploadFolder = Path.Combine("uploads", fileName);
                //            Directory.CreateDirectory("uploads");

                //            var filePath = Path.Combine(uploadFolder, $"{fileName}.part{chunkNumber}");
                //            await using var targetStream = System.IO.File.Create(filePath);
                //            await section.Body.CopyToAsync(targetStream);
                //        }
                //    }
                //}

                var fileSection = section.AsFileSection();
                if (fileSection != null)
                {
                    totalSizeInBytes += await SaveFileAsync(fileSection, filePaths, notUploadedFiles, direcory);
                    fileCount++;
                }

                section = await multipartReader.ReadNextSectionAsync();
            }

            return new FileUploadSummary
            {
                TotalFilesUploaded = fileCount,
                TotalSizeUploaded = ConvertSizeToString(totalSizeInBytes),
                FilePaths = filePaths,
                NotUploadedFiles = notUploadedFiles
            };
        }

        public async Task UploadFileChunkAsync(IFormFile file, string fileName, int chunkIndex)
        {
            var directory = Path.Combine("FilesUploadedChunk", fileName);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            //var mergeDirectory = Path.Combine("MergedChunk");
            //if (!Directory.Exists(mergeDirectory))
            //    Directory.CreateDirectory(mergeDirectory);

            // Temporary chunk file path
            var tempFilePath = Path.Combine(directory, "part_" + chunkIndex);

            // Save chunk
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

        }

        public async Task MergeChunks(string fileName)
        {
            var directory = Path.Combine("FilesUploadedChunk", fileName);
            var totalChunks = Directory.GetFiles(directory).Length;

            var mergedPath = Path.Combine("FilesUploadedChunk", DateTime.Now.ToString("ddMMyyyyHHmm") + "_" + fileName);
            using (var finalStream = new FileStream(mergedPath, FileMode.Create))
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    var partPath = Path.Combine(directory, "part_" + (i + 1));
                    if (File.Exists(partPath))
                    {
                        using (var partStream = new FileStream(partPath, FileMode.Open))
                        {
                            await partStream.CopyToAsync(finalStream);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Missing chunk {i} of {totalChunks}");
                    }
                }
            }

            Directory.Delete(directory, true);
        }

        private async Task<long> SaveFileAsync(FileMultipartSection fileSection, IList<string> filePaths, IList<string> notUploadedFiles, string directory)
        {
            try
            {
                var extension = Path.GetExtension(fileSection.FileName);
                if (!allowedExtensions.Contains(extension))
                {
                    notUploadedFiles.Add(fileSection.FileName);
                    return 0;
                }

                var filePath = Path.Combine(directory, fileSection?.FileName);
                await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096);
                await fileSection.FileStream?.CopyToAsync(stream);

                filePaths.Add(GetFullFilePath(fileSection, directory));

                Console.WriteLine("Osman:" + fileSection.FileStream.Length);
                return fileSection.FileStream.Length;

            }
            catch (Exception ex)
            {
                notUploadedFiles.Add(fileSection.FileName + " " + ex.Message);
                return 0;
            }
        }

        private string GetFullFilePath(FileMultipartSection fileSection, string directory)
        {
            return !string.IsNullOrEmpty(fileSection.FileName)
                ? Path.Combine(Directory.GetCurrentDirectory(), directory, fileSection.FileName)
                : string.Empty;
        }

        private string ConvertSizeToString(long bytes)
        {
            var fileSize = new decimal(bytes);
            var kilobyte = new decimal(1024);
            var megabyte = new decimal(1024 * 1024);
            var gigabyte = new decimal(1024 * 1024 * 1024);

            return fileSize switch
            {
                _ when fileSize < kilobyte => "Less then 1KB",
                _ when fileSize < megabyte =>
                    $"{Math.Round(fileSize / kilobyte, fileSize < 10 * kilobyte ? 2 : 1, MidpointRounding.AwayFromZero):##,###.##}KB",
                _ when fileSize < gigabyte =>
                    $"{Math.Round(fileSize / megabyte, fileSize < 10 * megabyte ? 2 : 1, MidpointRounding.AwayFromZero):##,###.##}MB",
                _ when fileSize >= gigabyte =>
                    $"{Math.Round(fileSize / gigabyte, fileSize < 10 * gigabyte ? 2 : 1, MidpointRounding.AwayFromZero):##,###.##}GB",
                _ => "n/a"
            };
        }

        private string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            return boundary;
        }
    }
}
