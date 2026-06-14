using Sayiad.Data.Common;

namespace Sayiad.Data.Repository.OrderRepo;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetAllOrdersAsync(PaginationRequest pagination);
    Task<PagedResult<Order>> GetUserOrdersAsync(int userId, PaginationRequest pagination);
    Task<IEnumerable<Order>> GetSellerOrdersAsync(int sellerId);
    Task<Order?> GetByIdAsync(int orderId, int userId);
    Task<Order?> GetByIdForAdminAsync(int orderId);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task<ShippingAddress?> GetShippingAddressAsync(int addressId, int userId);
    Task<Order> CreateOrderTransactionAsync(Order order, int userId);
    Task<List<Order>> GetPendingReturnRequestsAsync(DateTime cutoff);
}
