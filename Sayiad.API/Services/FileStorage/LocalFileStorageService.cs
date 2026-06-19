using Sayiad.Domain.Contracts;

namespace Sayiad.Api.Services.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _uploadsRoot;
    private bool _dirCreated;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _basePath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _uploadsRoot = Path.GetFullPath(Path.Combine(_basePath, "uploads"));
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder)
    {
        if (!_dirCreated)
        {
            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(_uploadsRoot);
            _dirCreated = true;
        }
        var flatFolder = SafeSegment(folder);
        var ext = Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var dir = GetSafeUploadPath(flatFolder);
        Directory.CreateDirectory(dir);
        var path = GetSafeUploadPath(flatFolder, uniqueName);
        await using var fs = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(fs);
        return $"/api/images/{flatFolder}/{uniqueName}";
    }

    public Task DeleteAsync(string url)
    {
        if (!url.StartsWith("/api/images/", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var parts = url["/api/images/".Length..]
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return Task.CompletedTask;

        var folder = SafeSegment(parts[0]);
        var fileName = Path.GetFileName(parts[1]);
        if (!string.Equals(fileName, parts[1], StringComparison.Ordinal))
            return Task.CompletedTask;

        var path = GetSafeUploadPath(folder, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static string SafeSegment(string value)
    {
        var segment = value
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace(':', '_');

        foreach (var invalid in Path.GetInvalidFileNameChars())
            segment = segment.Replace(invalid, '_');

        segment = segment.Trim('.', ' ', '_');
        return string.IsNullOrWhiteSpace(segment) ? "default" : segment;
    }

    private string GetSafeUploadPath(params string[] segments)
    {
        var fullPath = Path.GetFullPath(Path.Combine([_uploadsRoot, .. segments]));
        if (!fullPath.StartsWith(_uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, _uploadsRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid upload path.");
        return fullPath;
    }
}
