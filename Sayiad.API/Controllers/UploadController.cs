namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController(IFileStorageService fileStorage, ILogger<UploadController> logger) : ControllerBase
{
    private readonly IFileStorageService _fileStorage = fileStorage;
    private readonly ILogger<UploadController> _logger = logger;
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            if (file.Length > MaxFileSize)
                return BadRequest("File size exceeds 5 MB limit");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageTypes.ContainsKey(ext))
                return BadRequest("Only jpg, jpeg, png, gif, webp files are allowed");

            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                !string.Equals(file.ContentType, AllowedImageTypes[ext], StringComparison.OrdinalIgnoreCase))
                return BadRequest("File content does not match a valid image format.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            if (!IsValidImageBytes(memoryStream, ext))
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

    private static bool IsValidImageBytes(Stream stream, string extension)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var bytes = reader.ReadBytes(12);

        return extension switch
        {
            ".jpg" or ".jpeg" => bytes.Length >= 3 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,

            ".png" => bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 &&
                bytes[2] == 0x4E && bytes[3] == 0x47 &&
                bytes[4] == 0x0D && bytes[5] == 0x0A &&
                bytes[6] == 0x1A && bytes[7] == 0x0A,

            ".gif" => bytes.Length >= 6 &&
                bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 &&
                bytes[3] == 0x38 && (bytes[4] == 0x37 || bytes[4] == 0x39) &&
                bytes[5] == 0x61,

            ".webp" => bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 &&
                bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 &&
                bytes[10] == 0x42 && bytes[11] == 0x50,

            _ => false,
        };
    }
}
