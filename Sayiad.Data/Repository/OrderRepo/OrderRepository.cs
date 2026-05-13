using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Common;
using Sayiad.Data.Data;
using Sayiad.Data.Repository.OrderRepo;

namespace Sayiad.Data.Repository.OrderRepo;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _db;

    public OrderRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CustomerOrder>> GetUserOrdersAsync(int userId, PaginationRequest pagination)
    {
        var query = _db.CustomerOrders
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

        return new PagedResult<CustomerOrder>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<IEnumerable<CustomerOrder>> GetSellerOrdersAsync(int sellerId)
    {
        return await _db.CustomerOrders
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

    public async Task<CustomerOrder?> GetByIdAsync(int orderId)
    {
        return await _db.CustomerOrders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task AddAsync(CustomerOrder order)
    {
        _db.CustomerOrders.Add(order);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerOrder order)
    {
        _db.CustomerOrders.Update(order);
        await _db.SaveChangesAsync();
    }

    public async Task<ShippingAddress?> GetShippingAddressAsync(int addressId, int userId)
    {
        return await _db.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
    }

    public async Task<CustomerOrder> CreateOrderTransactionAsync(CustomerOrder order, int userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var item in order.OrderItems)
        {
            var product = await _db.Products.FindAsync(item.ProductId)
                ?? throw new InvalidOperationException($"Product {item.ProductId} not found");
            product.StockQuantity -= item.Quantity;
            if (product.StockQuantity == 0)
                product.Status = ProductStatus.Sold;
        }

        _db.CustomerOrders.Add(order);

        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null)
            _db.CartItems.RemoveRange(cart.CartItems);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return order;
    }
}
