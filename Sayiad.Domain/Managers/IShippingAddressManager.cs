using Sayiad.Domain.Dtos.ShippingAddressDtos;

namespace Sayiad.Domain.Managers;

public interface IShippingAddressManager
{
    Task<ShippingAddressResponse> CreateAsync(int userId, CreateShippingAddressRequest request);
    Task<List<ShippingAddressResponse>> GetMyAddressesAsync(int userId);
    Task<ShippingAddressResponse?> UpdateAsync(int userId, int addressId, CreateShippingAddressRequest request);
    Task<bool> DeleteAsync(int userId, int addressId);
}
