using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.ShippingAddressDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShippingAddressesController : ControllerBase
{
    private readonly IShippingAddressManager _shippingAddressManager;

    public ShippingAddressesController(IShippingAddressManager shippingAddressManager)
    {
        _shippingAddressManager = shippingAddressManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateShippingAddressRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var address = await _shippingAddressManager.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetMyAddresses), new { }, address);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var addresses = await _shippingAddressManager.GetMyAddressesAsync(userId);
        return Ok(addresses);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _shippingAddressManager.DeleteAsync(userId, id);
        return NoContent();
    }
}
