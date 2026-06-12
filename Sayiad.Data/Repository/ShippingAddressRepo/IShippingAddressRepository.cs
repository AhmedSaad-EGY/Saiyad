namespace Sayiad.Data.Repository.ShippingAddressRepo;

public interface IShippingAddressRepository
{
    Task<ShippingAddress> CreateAsync(ShippingAddress address);
    Task<ShippingAddress?> GetByIdAsync(int id, int userId);
    Task<List<ShippingAddress>> GetByUserIdAsync(int userId);
    Task<ShippingAddress?> UpdateAsync(ShippingAddress address);
    Task<bool> DeleteAsync(int id, int userId);
}
