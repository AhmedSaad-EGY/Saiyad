using Sayiad.Data.Common;
using Sayiad.Data.Models;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Domain.Managers;

public interface IOrderManager
{
    Task<OrderResponse> CreateFromCartAsync(int userId, CreateOrderRequest request);
    Task<PagedResult<OrderResponse>> GetAllOrdersAsync(PaginationRequest? pagination = null);
    Task<PagedResult<OrderResponse>> GetUserOrdersAsync(int userId, PaginationRequest? pagination = null);
    Task<IEnumerable<OrderResponse>> GetSellerOrdersAsync(int sellerId);
    Task<OrderResponse> GetByIdAsync(int orderId, int userId);
    Task<OrderResponse> UpdateStatusAsync(int orderId, OrderStatus status, int? updatedByUserId = null);
    Task<OrderResponse> CancelAsync(int orderId, int userId);
    Task<OrderResponse> RequestReturnAsync(int orderId, int userId);
    Task<OrderResponse> ApproveReturnAsync(int orderId, int adminId);
    Task<OrderResponse> RejectReturnAsync(int orderId, int adminId, string reason);
}
