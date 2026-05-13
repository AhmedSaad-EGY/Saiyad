using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Contracts;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Fisherman,BaitSeller")]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public UploadController(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        if (file.Length > MaxFileSize)
            return BadRequest("File size exceeds 5 MB limit");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest("Only jpg, jpeg, png, webp files are allowed");

        if (!IsValidImageBytes(file))
            return BadRequest("File content does not match a valid image format.");

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadAsync(stream, file.FileName, "sayiad/products");
        return Ok(new { url });
    }

    private static bool IsValidImageBytes(IFormFile file)
    {
        using var reader = new BinaryReader(file.OpenReadStream());
        var bytes = reader.ReadBytes(4);

        // JPEG: FF D8 FF
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47
        if (bytes.Length >= 4 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 &&
            bytes[2] == 0x4E && bytes[3] == 0x47)
            return true;

        // WebP: RIFF....WEBP — first 4 bytes are 52 49 46 46
        if (bytes.Length >= 4 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 &&
            bytes[2] == 0x46 && bytes[3] == 0x46)
            return true;

        return false;
    }
}
