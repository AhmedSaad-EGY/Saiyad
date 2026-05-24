using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sayiad.Tests.Managers;

public class ProductManagerTests
{
    private readonly Mock<IProductRepository> _repoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<ProductManager>> _loggerMock = new();
    private ProductManager CreateManager() =>
        new(_repoMock.Object, _categoryRepoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Update_ByNonOwner_ThrowsUnauthorizedAccessException()
    {
        var product = new Product { Id = 1, SellerId = 99 };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        var manager = CreateManager();

        var act = () => manager.UpdateAsync(1, sellerId: 1,
            new UpdateProductRequest("t", "d", "b", 0, 9.99m, 1, "loc", 1, 0));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);
        var manager = CreateManager();

        var act = () => manager.GetByIdAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
