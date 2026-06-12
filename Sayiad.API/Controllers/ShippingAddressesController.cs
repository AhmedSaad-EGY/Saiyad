namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
public class ShippingAddressesController : BaseController
{
    private readonly IShippingAddressManager _shippingAddressManager;

    public ShippingAddressesController(IShippingAddressManager shippingAddressManager)
    {
        _shippingAddressManager = shippingAddressManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateShippingAddressRequest request)
    {
        var userId = GetUserId();
        var address = await _shippingAddressManager.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetMyAddresses), new { }, address);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = GetUserId();
        var addresses = await _shippingAddressManager.GetMyAddressesAsync(userId);
        return Ok(addresses);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CreateShippingAddressRequest request)
    {
        var userId = GetUserId();
        var address = await _shippingAddressManager.UpdateAsync(userId, id, request);
        if (address is null) return NotFound();
        return Ok(address);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        await _shippingAddressManager.DeleteAsync(userId, id);
        return NoContent();
    }
}
