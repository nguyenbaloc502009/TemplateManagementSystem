using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace TemplateManagement.Core.Storage;

public sealed class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient _client;
    private readonly string _bucket;

    public MinioObjectStorage(IConfiguration configuration)
    {
        string endpoint = Required(configuration, "ObjectStorage:Endpoint");
        string accessKey = Required(configuration, "ObjectStorage:AccessKey");
        string secretKey = Required(configuration, "ObjectStorage:SecretKey");
        _bucket = Required(configuration, "ObjectStorage:Bucket");
        bool useSsl = configuration.GetValue<bool>("ObjectStorage:UseSSL");

        _client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSsl)
            .Build();

        bool bucketExists = _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket)).GetAwaiter().GetResult();
        if (!bucketExists)
        {
            _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket))
                .GetAwaiter().GetResult();
        }
    }

    /// <summary>Tải nội dung lên bucket MinIO dưới object key đã kiểm tra.</summary>
    public void Put(string objectKey, byte[] contents, string contentType)
    {
        ValidateObjectKey(objectKey);
        ArgumentNullException.ThrowIfNull(contents);

        using var stream = new MemoryStream(contents, writable: false);
        _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType))
            .GetAwaiter().GetResult();
    }

    /// <summary>Kiểm tra object có tồn tại; chỉ trả false khi MinIO báo không tìm thấy.</summary>
    public bool Exists(string objectKey)
    {
        ValidateObjectKey(objectKey);
        try
        {
            _client.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectKey))
                .GetAwaiter().GetResult();
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    /// <summary>Tải object về bộ nhớ và trả về toàn bộ nội dung byte.</summary>
    public byte[] Get(string objectKey)
    {
        ValidateObjectKey(objectKey);

        using var output = new MemoryStream();
        _client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucket)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(output)))
            .GetAwaiter().GetResult();
        return output.ToArray();
    }

    /// <summary>Xóa object khỏi bucket MinIO.</summary>
    public void Delete(string objectKey)
    {
        ValidateObjectKey(objectKey);
        _client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucket)
                .WithObject(objectKey))
            .GetAwaiter().GetResult();
    }

    /// <summary>Đọc giá trị cấu hình bắt buộc và báo lỗi nếu thiếu hoặc rỗng.</summary>
    private static string Required(IConfiguration configuration, string key)
    {
        string? value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Object storage configuration '{key}' is required.");
        }
        return value.Trim();
    }

    /// <summary>Từ chối object key tuyệt đối hoặc chứa phân đoạn đường dẫn không an toàn.</summary>
    private static void ValidateObjectKey(string objectKey)
    {
        // Không cho phép traversal hoặc dấu phân cách khác quy ước để khóa luôn nằm trong bucket.
        if (string.IsNullOrWhiteSpace(objectKey)
            || objectKey.StartsWith('/')
            || objectKey.Contains('\\')
            || objectKey.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException("Object key must be a normalized relative key.", nameof(objectKey));
        }
    }
}
