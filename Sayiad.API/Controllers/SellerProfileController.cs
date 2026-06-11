namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/seller-profile")]
public class SellerProfileController : BaseController
{
    private readonly ISellerProfileManager _sellerProfileManager;

    public SellerProfileController(ISellerProfileManager sellerProfileManager)
    {
        _sellerProfileManager = sellerProfileManager;
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> Create(CreateSellerProfileRequest request)
    {
        var userId = GetUserId();
        var profile = await _sellerProfileManager.CreateAsync(userId, request);
        return StatusCode(201, profile);
    }

    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var profile = await _sellerProfileManager.GetByUserIdAsync(userId);
        return Ok(profile);
    }

    [HttpPut]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> Update(UpdateSellerProfileRequest request)
    {
        var userId = GetUserId();
        var profile = await _sellerProfileManager.UpdateAsync(userId, request);
        return Ok(profile);
    }

    [HttpGet("me")]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserId();
        var profile = await _sellerProfileManager.GetMyProfileAsync(userId);
        return Ok(profile);
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserId();
        var dashboard = await _sellerProfileManager.GetDashboardAsync(userId);
        return Ok(dashboard);
    }
}
