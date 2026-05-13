namespace Sayiad.Data.Repository.CategoryRepo;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<Category?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<bool> HasProductsAsync(int id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);
}
