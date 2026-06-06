using Sayiad.Domain.Contracts;

namespace Sayiad.Api.Services.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private bool _dirCreated;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _basePath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder)
    {
        if (!_dirCreated)
        {
            Directory.CreateDirectory(_basePath);
            _dirCreated = true;
        }
        var flatFolder = folder.Replace('/', '_');
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var relative = Path.Combine("uploads", flatFolder, uniqueName);
        var dir = Path.Combine(_basePath, Path.GetDirectoryName(relative)!);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(_basePath, relative);
        await using var fs = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(fs);
        return $"/api/images/{flatFolder}/{uniqueName}";
    }

    public Task DeleteAsync(string url)
    {
        // url = /api/images/{folder}/{fileName}, convert to uploads/{folder}/{fileName}
        var relative = "uploads" + url["/api/images".Length..];
        var path = Path.Combine(_basePath, relative.TrimStart('/'));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
