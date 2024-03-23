using Amazon.S3.Model;

namespace TaskNinjaHub.FileStorage.WebApi.Inrefaces;

public interface IStorageService
{
    Task UploadFileAsync(Stream stream, string key);

    Task<Stream> DownloadFileAsync(string key);

    Task DeleteFileAsync(string key);

    Task<ListObjectsV2Response> ListFilesAsync();
}