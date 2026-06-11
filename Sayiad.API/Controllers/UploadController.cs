namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<UploadController> _logger;
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public UploadController(IFileStorageService fileStorage, ILogger<UploadController> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            if (file.Length > MaxFileSize)
                return BadRequest("File size exceeds 5 MB limit");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest("Only jpg, jpeg, png, webp files are allowed");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            if (!IsValidImageBytes(memoryStream))
                return BadRequest("File content does not match a valid image format.");

            memoryStream.Position = 0;
            var safeName = $"{Guid.NewGuid()}{ext}";
            var url = await _fileStorage.UploadAsync(memoryStream, safeName, "sayiad/profiles");
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image upload failed");
            return StatusCode(500, new { message = "Image upload failed. Please try again." });
        }
    }

    private static bool IsValidImageBytes(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
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
