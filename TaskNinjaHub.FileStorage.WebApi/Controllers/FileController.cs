using System.Reactive.Linq;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;
using Minio.Exceptions;
using TaskNinjaHub.FileStorage.WebApi.Data;

namespace TaskNinjaHub.FileStorage.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController(IMinioClient minioClient) : ControllerBase
{
    /// <summary>
    /// Метод получения всех бакетов
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetAllBuckets")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBuckets()
    {
        try
        {
            List<Bucket> bucketList = (await minioClient.ListBucketsAsync()).Buckets.ToList();
            return Ok(bucketList);
        }
        catch (MinioException exception)
        {
            return BadRequest(exception);
        }
    }

    /// <summary>
    /// Метод удаления бакета, принимает значение имени бакета
    /// Если бакет не пустой, то вернется ошибка
    /// МОЖНО УДАЛЯТЬ ТОЛЬКО ПУСТЫЕ БАКЕТЫ
    /// </summary>
    /// <param name="bucketName">Имя бакета</param>
    /// <returns></returns>
    [HttpPost("DeleteBucket")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteBucket(string bucketName)
    {
        try
        {
            bool found = await minioClient.BucketExistsAsync(new BucketExistsArgs()
                .WithBucket(bucketName));

            if (found)
            {
                var args = new ListObjectsArgs()
                    .WithBucket(bucketName)
                    .WithRecursive(true);
                var observable = await minioClient.ListObjectsAsync(args).ToList();

                if (observable.Count == 0)
                {
                    await minioClient.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(bucketName));
                    return Ok($"{bucketName} is deleted");
                }
                else
                    return BadRequest("Bucket in not empty");
            }
            else
            {
                return BadRequest($"{bucketName} is doesnt exist");
            }
        }
        catch (MinioException e)
        {
            return BadRequest(e);
        }
    }

    /// <summary>
    /// Метод добавления бакета, принимает значение имени бакета
    /// </summary>
    /// <param name="bucketName">Имя бакета</param>
    /// <returns></returns>
    [HttpPost("AddBucket")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddBucket(string bucketName)
    {
        try
        {
            bool found = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (found)
            {
                return BadRequest("Bucket already exist");
            }
            else
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                return Ok($"{bucketName} is created");
            }
        }
        catch (MinioException e)
        {
            return BadRequest(e);
        }
    }

    /// <summary>
    /// Метод получения всех фаайлов файлов внутри бакета, принимает значение имени бакета
    /// </summary>
    /// <param name="bucketName">Имя бакета</param>
    /// <returns></returns>
    [HttpGet("GetAllBucketFiles")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBucketFiles(string bucketName)
    {
        try
        {
            var found = await minioClient.BucketExistsAsync(new BucketExistsArgs()
                .WithBucket(bucketName));

            if (found)
            {
                var args = new ListObjectsArgs()
                    .WithBucket(bucketName)
                    .WithRecursive(true);
                var observable = await minioClient.ListObjectsAsync(args).ToList();
                return Ok(observable);
            }
            else
                return BadRequest("Бакет не существует");
        }
        catch (MinioException e)
        {
            return BadRequest(e);
        }
    }

    /// <summary>
    /// Метод загрузки файла с бакета
    /// </summary>
    /// <param name="bucketName">Имя бакета</param>
    /// <param name="fileName">Полное имя получаемого файла (example.txt)</param>
    /// <returns></returns>
    [HttpGet("GetFile")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFile(string bucketName, string fileName)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName);
            await minioClient.StatObjectAsync(statObjectArgs);

            var tempFilePath = Path.GetTempFileName();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithFile(tempFilePath);

            var stream = await minioClient.GetObjectAsync(getObjectArgs);
            Stream s = new FileStream(tempFilePath, FileMode.Open);

            return new FileStreamResult(s, stream.ContentType)
            {
                FileDownloadName = fileName
            };
        }
        catch (MinioException e)
        {
            return BadRequest(e);
        }
    }

    /// <summary>
    /// Метод удаления файла с бакета
    /// </summary>
    /// <param name="bucketName">Имя бакета</param>
    /// <param name="fileName">Полное имя удаляемого файла (example.txt)</param>
    /// <returns></returns>
    [HttpPost("DeleteFile")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteFile(string bucketName, string fileName)
    {
        try
        {
            RemoveObjectArgs args = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName);
            await minioClient.RemoveObjectAsync(args);
            return Ok($"The file {fileName} is deleted");
        }
        catch (MinioException e)
        {
            return BadRequest(e);
        }
    }

    /// <summary>
    /// Метод выгрузки(отпрвки) файла на сервер
    /// При отправке одного и того же файла, он будет перезаписываться
    /// </summary>
    /// <param name="bucketName">Имя бакета</param>
    /// <param name="file">Загружаемый файл(IFormFile)</param>
    /// <returns></returns>
    [HttpPost("UploadFile")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile(string bucketName, [FromForm] FileUploadRequest file)
    {
        try
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.File?.CopyToAsync(memoryStream)!;

                memoryStream.Position = 0;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(file.File.FileName)
                    .WithContentType(file.File.ContentType)
                    .WithStreamData(memoryStream)
                    .WithObjectSize(file.File.Length);

                await minioClient.PutObjectAsync(putObjectArgs);
            }

            return Ok($"File {file.File.FileName} is uploaded to Minio");
        }
        catch (MinioException e)
        {
            return BadRequest(e);
        }
    }
}