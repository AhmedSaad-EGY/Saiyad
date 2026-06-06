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
        [".webp"] = "image/webp",
    };

    public ImageController(IWebHostEnvironment env) => _env = env;

    [HttpGet("{folder}/{fileName}")]
    public IActionResult Get(string folder, string fileName)
    {
        var basePath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var path = Path.Combine(basePath, "uploads", folder, fileName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        var ext = Path.GetExtension(fileName);
        var contentType = MimeMap.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";

        return PhysicalFile(path, contentType);
    }
}
