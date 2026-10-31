using System.Collections.Concurrent;
using TemplateManagement.Core.Storage;

namespace TemplateSystem.Tests;

internal sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

    /// <summary>Kiểm tra object key có dữ liệu trong kho giả lập hay không.</summary>
    public bool Exists(string objectKey) => _objects.ContainsKey(objectKey);

    /// <summary>Lưu bản sao nội dung để test không chia sẻ mảng byte bên ngoài.</summary>
    public void Put(string objectKey, byte[] contents, string contentType) =>
        _objects[objectKey] = contents.ToArray();

    /// <summary>Trả bản sao nội dung hoặc báo lỗi nếu object không tồn tại.</summary>
    public byte[] Get(string objectKey) =>
        _objects.TryGetValue(objectKey, out byte[]? contents)
            ? contents.ToArray()
            : throw new FileNotFoundException($"Object '{objectKey}' does not exist.");

    /// <summary>Xóa object key khỏi kho giả lập nếu có.</summary>
    public void Delete(string objectKey) => _objects.TryRemove(objectKey, out _);
}
