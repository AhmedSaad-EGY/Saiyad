using Sayiad.Domain.Contracts;

namespace Sayiad.Api.Services.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _basePath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var relative = Path.Combine("uploads", folder, uniqueName);
        var dir = Path.Combine(_basePath, Path.GetDirectoryName(relative)!);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(_basePath, relative);
        await using var fs = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(fs);
        return $"/{relative.Replace('\\', '/')}";
    }

    public Task DeleteAsync(string url)
    {
        var path = Path.Combine(_basePath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
