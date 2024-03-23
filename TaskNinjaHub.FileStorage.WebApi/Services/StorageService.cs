using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using TaskNinjaHub.FileStorage.WebApi.Inrefaces;

namespace TaskNinjaHub.FileStorage.WebApi.Services;

public class StorageService(IAmazonS3 s3Client, string bucketName = "tnh-file-storage") : IStorageService
{
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private readonly string _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));

    public async Task UploadFileAsync(Stream stream, string key)
    {
        try
        {
            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(stream, _bucketName, key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string key)
    {
        try
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(getObjectRequest);
            return response.ResponseStream;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteFileAsync(string key)
    {
        try
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteObjectRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file: {ex.Message}");
            throw;
        }
    }

    public async Task<ListObjectsV2Response> ListFilesAsync()
    {
        try
        {
            var listObjectsRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName
            };

            return await _s3Client.ListObjectsV2Async(listObjectsRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing files: {ex.Message}");
            throw;
        }
    }
}