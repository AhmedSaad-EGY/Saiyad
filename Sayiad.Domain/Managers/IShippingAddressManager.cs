using Sayiad.Domain.Dtos.ShippingAddressDtos;

namespace Sayiad.Domain.Managers;

public interface IShippingAddressManager
{
    Task<ShippingAddressResponse> CreateAsync(int userId, CreateShippingAddressRequest request);
    Task<List<ShippingAddressResponse>> GetMyAddressesAsync(int userId);
    Task<bool> DeleteAsync(int userId, int addressId);
}
