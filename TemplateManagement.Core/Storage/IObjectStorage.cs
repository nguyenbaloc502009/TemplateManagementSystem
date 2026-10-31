namespace TemplateManagement.Core.Storage;

public interface IObjectStorage
{
    bool Exists(string objectKey);
    void Put(string objectKey, byte[] contents, string contentType);
    byte[] Get(string objectKey);
    void Delete(string objectKey);
}
