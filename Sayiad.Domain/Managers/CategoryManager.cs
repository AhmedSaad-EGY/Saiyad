using Microsoft.Extensions.Logging;
using Sayiad.Domain.Dtos.CategoryDtos;

namespace Sayiad.Domain.Managers;

public class CategoryManager : ICategoryManager
{
    private readonly ICategoryRepository _repo;
    private readonly ILogger<CategoryManager> _logger;

    public CategoryManager(ICategoryRepository repo, ILogger<CategoryManager> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
    {
        var categories = await _repo.GetAllAsync();
        return categories.Select(c => new CategoryResponse(
            c.Id, c.Name, c.Description, c.CreatedAt, c.Products.Count));
    }

    public async Task<CategoryResponse> GetByIdAsync(int id)
    {
        var category = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category not found");
        return new CategoryResponse(
            category.Id, category.Name, category.Description,
            category.CreatedAt, category.Products.Count);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        if (await _repo.ExistsByNameAsync(request.Name))
            throw new InvalidOperationException("Category name already exists");

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(category);
        _logger.LogInformation("Category created: {CategoryName}", category.Name);

        return new CategoryResponse(
            category.Id, category.Name, category.Description,
            category.CreatedAt, 0);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category not found");

        if (await _repo.ExistsByNameAsync(request.Name, id))
            throw new InvalidOperationException("Category name already exists");

        category.Name = request.Name;
        category.Description = request.Description;

        await _repo.UpdateAsync(category);
        _logger.LogInformation("Category updated: {CategoryId}", id);

        return new CategoryResponse(
            category.Id, category.Name, category.Description,
            category.CreatedAt, category.Products.Count);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category not found");

        if (await _repo.HasProductsAsync(id))
            throw new InvalidOperationException("Cannot delete category with existing products");

        await _repo.DeleteAsync(category);
        _logger.LogInformation("Category deleted: {CategoryId}", id);
    }
}