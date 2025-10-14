using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Core.Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Core.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _bucketName;

        public FileStorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
            _bucketName = _configuration["AWS:S3:BucketName"] ?? throw new Exception("AWS S3 BucketName not configured");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty or null");

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var key = $"{folder}/{fileName}";

                using (var stream = file.OpenReadStream())
                {
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = key,
                        BucketName = _bucketName,
                        ContentType = file.ContentType,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    var transferUtility = new TransferUtility(_s3Client);
                    await transferUtility.UploadAsync(uploadRequest);
                }

                return GetFileUrl(fileName, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to S3");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string folder, string contentType)
        {
            try
            {
                if (fileBytes == null || fileBytes.Length == 0)
                    throw new ArgumentException("File bytes are empty or null");

                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var key = $"{folder}/{uniqueFileName}";

                using (var stream = new MemoryStream(fileBytes))
                {
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = key,
                        BucketName = _bucketName,
                        ContentType = contentType,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    var transferUtility = new TransferUtility(_s3Client);
                    await transferUtility.UploadAsync(uploadRequest);
                }

                return GetFileUrl(uniqueFileName, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file bytes to S3");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileUrl))
                    return false;

                // Extract key from URL
                var key = ExtractKeyFromUrl(fileUrl);
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from S3: {FileUrl}", fileUrl);
                return false;
            }
        }

        public async Task<byte[]> DownloadFileAsync(string fileUrl)
        {
            try
            {
                var key = ExtractKeyFromUrl(fileUrl);
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("Invalid file URL");

                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                using (var response = await _s3Client.GetObjectAsync(request))
                using (var memoryStream = new MemoryStream())
                {
                    await response.ResponseStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from S3: {FileUrl}", fileUrl);
                throw;
            }
        }

        public string GetFileUrl(string fileName, string folder)
        {
            var region = _configuration["AWS:S3:Region"] ?? "us-east-1";
            return $"https://{_bucketName}.s3.{region}.amazonaws.com/{folder}/{fileName}";
        }

        private string ExtractKeyFromUrl(string fileUrl)
        {
            try
            {
                var uri = new Uri(fileUrl);
                // Remove leading slash
                return uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
