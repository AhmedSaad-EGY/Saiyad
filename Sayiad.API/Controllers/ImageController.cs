using Microsoft.AspNetCore.Mvc;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/images")]
[AllowAnonymous]
public class ImageController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private static readonly Dictionary<string, string> MimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    public ImageController(IWebHostEnvironment env) => _env = env;

    [HttpGet("{folder}/{fileName}")]
    public IActionResult Get(string folder, string fileName)
    {
        if (folder.Contains("..") || fileName.Contains("..") ||
            folder.Contains("/") || fileName.Contains("/") ||
            folder.Contains("\\") || fileName.Contains("\\") ||
            folder.Contains(":") || fileName.Contains(":"))
            return BadRequest("Invalid path");

        var ext = Path.GetExtension(fileName);
        if (!MimeMap.ContainsKey(ext))
            return BadRequest("Unsupported file type");

        var basePath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.GetFullPath(Path.Combine(basePath, "uploads", folder, fileName));
        var uploadsDir = Path.GetFullPath(Path.Combine(basePath, "uploads"));

        if (!fullPath.StartsWith(uploadsDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Invalid path");

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        return PhysicalFile(fullPath, MimeMap[ext]);
    }
}
