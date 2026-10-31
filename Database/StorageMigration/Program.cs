using Microsoft.Extensions.Configuration;
using TemplateManagement.Core.Storage;
using System.Security.Cryptography;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ObjectStorage:Endpoint"] = "localhost:9000",
        ["ObjectStorage:Bucket"] = "template-management",
        ["ObjectStorage:UseSSL"] = "false"
    })
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var objectStorage = new MinioObjectStorage(configuration);
string repositoryRoot = FindRepositoryRoot();
int uploaded = 0;

foreach (string project in new[] { "DocumentTemplateSystem", "PresentationTemplateSystem" })
{
    string storageRoot = Path.Combine(repositoryRoot, project, "storage");
    if (!Directory.Exists(storageRoot))
    {
        continue;
    }

    foreach (string filePath in Directory.EnumerateFiles(storageRoot, "*", SearchOption.AllDirectories))
    {
        string relativePath = Path.GetRelativePath(storageRoot, filePath).Replace('\\', '/');
        string objectKey = $"storage/{relativePath}";
        string contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
        byte[] source = File.ReadAllBytes(filePath);

        objectStorage.Put(objectKey, source, contentType);
        byte[] uploadedContents = objectStorage.Get(objectKey);
        if (!CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(source),
                SHA256.HashData(uploadedContents)))
        {
            throw new IOException($"Uploaded object verification failed for '{objectKey}'.");
        }

        uploaded++;
        Console.WriteLine($"Uploaded and verified {objectKey} ({source.Length} bytes).");
    }
}

Console.WriteLine($"Completed: {uploaded} file(s) uploaded and verified. Local source files were retained.");

static string FindRepositoryRoot()
{
    for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "TemplateManagementSystem.sln"))
            && Directory.Exists(Path.Combine(directory.FullName, "DocumentTemplateSystem"))
            && Directory.Exists(Path.Combine(directory.FullName, "PresentationTemplateSystem")))
        {
            return directory.FullName;
        }
    }

    throw new DirectoryNotFoundException("Could not locate the solution root for storage migration.");
}
