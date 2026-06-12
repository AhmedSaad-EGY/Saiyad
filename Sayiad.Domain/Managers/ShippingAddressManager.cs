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

    public async Task<ShippingAddressResponse?> UpdateAsync(int userId, int addressId, CreateShippingAddressRequest request)
    {
        var existing = await _repo.GetByIdAsync(addressId, userId);
        if (existing is null) return null;

        existing.FullName = request.FullName;
        existing.Phone = request.Phone;
        existing.City = request.City;
        existing.AddressLine = request.AddressLine;
        existing.PostalCode = request.PostalCode ?? string.Empty;

        var updated = await _repo.UpdateAsync(existing);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int userId, int addressId)
    {
        return await _repo.DeleteAsync(addressId, userId);
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
