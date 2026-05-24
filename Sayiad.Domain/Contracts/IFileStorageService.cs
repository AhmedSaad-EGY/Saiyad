namespace Sayiad.Domain.Contracts;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string folder);
    Task DeleteAsync(string publicId);
}
