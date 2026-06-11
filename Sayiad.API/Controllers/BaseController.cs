namespace Sayiad.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !int.TryParse(value, out var id))
            throw new UnauthorizedAccessException("Invalid user identity");
        return id;
    }
}
