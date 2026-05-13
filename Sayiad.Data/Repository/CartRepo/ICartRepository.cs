namespace Sayiad.Data.Repository.CartRepo;

public interface ICartRepository
{
    Task<Cart?> GetCartAsync(int userId);
    Task AddAsync(Cart cart);
    Task ClearCartAsync(int userId);
    Task SaveChangesAsync();
}
