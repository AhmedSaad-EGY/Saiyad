using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AntiForgeryController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiForgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    [HttpGet("token")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}
