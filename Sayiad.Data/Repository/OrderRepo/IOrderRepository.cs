using Sayiad.Data.Common;

namespace Sayiad.Data.Repository.OrderRepo;

public interface IOrderRepository
{
    Task<PagedResult<CustomerOrder>> GetUserOrdersAsync(int userId, PaginationRequest pagination);
    Task<IEnumerable<CustomerOrder>> GetSellerOrdersAsync(int sellerId);
    Task<CustomerOrder?> GetByIdAsync(int orderId);
    Task AddAsync(CustomerOrder order);
    Task UpdateAsync(CustomerOrder order);
    Task<ShippingAddress?> GetShippingAddressAsync(int addressId, int userId);
    Task<CustomerOrder> CreateOrderTransactionAsync(CustomerOrder order, int userId);
}
