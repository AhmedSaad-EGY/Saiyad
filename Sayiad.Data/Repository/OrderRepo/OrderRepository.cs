using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Common;
using Sayiad.Data.Data;
using Sayiad.Data.Models;

namespace Sayiad.Data.Repository.OrderRepo;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _db;

    public OrderRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Order>> GetAllOrdersAsync(PaginationRequest pagination)
    {
        var query = _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<Order>> GetUserOrdersAsync(int userId, PaginationRequest pagination)
    {
        var query = _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .Where(o => o.BuyerId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<IEnumerable<Order>> GetSellerOrdersAsync(int sellerId)
    {
        return await _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .Where(o => o.OrderItems.Any(oi => oi.SellerId == sellerId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int orderId, int userId)
    {
        return await _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == orderId &&
                (o.BuyerId == userId || o.OrderItems.Any(oi => oi.SellerId == userId)));
    }

    public async Task<Order?> GetByIdForAdminAsync(int orderId)
    {
        return await _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task AddAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _db.Orders.Update(order);
        await _db.SaveChangesAsync();
    }

    public async Task<ShippingAddress?> GetShippingAddressAsync(int addressId, int userId)
    {
        return await _db.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
    }

    public async Task<List<Order>> GetPendingReturnRequestsAsync(DateTime cutoff)
    {
        return await _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .Where(o => o.Status == OrderStatus.ReturnRequested
                     && o.DeliveredAt.HasValue
                     && o.DeliveredAt.Value < cutoff)
            .ToListAsync();
    }

    public async Task<Order> CreateOrderTransactionAsync(Order order, int userId)
    {
        foreach (var item in order.OrderItems)
        {
            var product = await _db.Products.FindAsync(item.ProductId)
                ?? throw new InvalidOperationException($"Product {item.ProductId} not found");
            product.StockQuantity -= item.Quantity;
            if (product.StockQuantity == 0)
                product.Status = ProductStatus.Sold;
        }

        _db.Orders.Add(order);

        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null)
            _db.CartItems.RemoveRange(cart.CartItems);

        return order;
    }
}
