using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Data;
using Sayiad.Data.Repository.CategoryRepo;

namespace Sayiad.Data.Repository.CategoryRepo;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _db;

    public CategoryRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _db.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _db.Categories
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        if (excludeId.HasValue)
            return await _db.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId.Value);
        return await _db.Categories.AnyAsync(c => c.Name == name);
    }

    public async Task<bool> HasProductsAsync(int id)
    {
        return await _db.Products.AnyAsync(p => p.CategoryId == id);
    }

    public async Task AddAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Category category)
    {
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }
}
