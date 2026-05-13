using Sayiad.Domain.Dtos.ShippingAddressDtos;

namespace Sayiad.Domain.Managers;

public class ShippingAddressManager : IShippingAddressManager
{
    private readonly IShippingAddressRepository _repo;

    public ShippingAddressManager(IShippingAddressRepository repo)
    {
        _repo = repo;
    }

    public async Task<ShippingAddressResponse> CreateAsync(int userId, CreateShippingAddressRequest request)
    {
        var address = new ShippingAddress
        {
            UserId = userId,
            FullName = request.FullName,
            Phone = request.Phone,
            City = request.City,
            AddressLine = request.AddressLine,
            PostalCode = request.PostalCode ?? string.Empty,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(address);
        return MapToResponse(created);
    }

    public async Task<bool> DeleteAsync(int userId, int addressId)
    {
        var address = await _repo.GetByIdAsync(addressId)
            ?? throw new KeyNotFoundException("Shipping address not found");

        if (address.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own addresses.");

        return await _repo.DeleteAsync(addressId);
    }

    public async Task<List<ShippingAddressResponse>> GetMyAddressesAsync(int userId)
    {
        var addresses = await _repo.GetByUserIdAsync(userId);
        return addresses.Select(MapToResponse).ToList();
    }

    private static ShippingAddressResponse MapToResponse(ShippingAddress a)
    {
        return new ShippingAddressResponse(
            a.Id, a.FullName, a.Phone, a.City, a.AddressLine,
            a.PostalCode, a.IsDefault, a.CreatedAt
        );
    }
}
