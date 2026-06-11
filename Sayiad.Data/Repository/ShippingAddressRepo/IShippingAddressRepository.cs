namespace Sayiad.Data.Repository.ShippingAddressRepo;

public interface IShippingAddressRepository
{
    Task<ShippingAddress> CreateAsync(ShippingAddress address);
    Task<ShippingAddress?> GetByIdAsync(int id, int userId);
    Task<List<ShippingAddress>> GetByUserIdAsync(int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
