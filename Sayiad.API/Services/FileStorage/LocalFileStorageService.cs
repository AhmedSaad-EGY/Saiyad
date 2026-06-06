using Sayiad.Domain.Contracts;

namespace Sayiad.Api.Services.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var relative = Path.Combine("uploads", folder, uniqueName);
        var path = Path.Combine(_env.WebRootPath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var fs = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(fs);
        return $"/{relative.Replace('\\', '/')}";
    }

    public Task DeleteAsync(string url)
    {
        var path = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
