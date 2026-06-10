using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sayiad.Tests.Managers;

public class ProductManagerTests
{
    private readonly Mock<IProductRepository> _repoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<ProductManager>> _loggerMock = new();
    private readonly Mock<IWalletManager> _walletManagerMock = new();
    private readonly Mock<INotificationManager> _notifMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private ProductManager CreateManager() =>
        new(_repoMock.Object, _categoryRepoMock.Object, _loggerMock.Object, _walletManagerMock.Object, _notifMock.Object, _userRepoMock.Object);

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

    [Fact]
    public async Task CreateAsync_HoldsFivePercentOfPrice()
    {
        var category = new Category { Id = 1, Name = "Test" };
        var product = new Product { Id = 1, SellerId = 7, Price = 100m, Status = ProductStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Category = new Category { Id = 1, Name = "Test" } };
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Callback<Product>(p => p.Id = 1).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        await CreateManager().CreateAsync(7, new CreateProductRequest("T", "D", "B", ProductCondition.New, 100m, 10, "Loc", 1));

        _walletManagerMock.Verify(w => w.HoldFundsAsync(7, 5m, "Product", 1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InsufficientFunds_ThrowsInvalidOperationException()
    {
        var category = new Category { Id = 1 };
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Callback<Product>(p => p.Id = 1).Returns(Task.CompletedTask);
        _walletManagerMock.Setup(w => w.HoldFundsAsync(7, 5m, "Product", 1)).ThrowsAsync(new InvalidOperationException("Insufficient funds"));

        var act = () => CreateManager().CreateAsync(7, new CreateProductRequest("T", "D", "B", ProductCondition.New, 100m, 10, "Loc", 1));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Insufficient funds");
    }

    [Fact]
    public async Task DeleteAsync_ReleasesFivePercentHold()
    {
        var product = new Product { Id = 1, SellerId = 7, Price = 100m, Category = new Category { Id = 1, Name = "Test" } };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        await CreateManager().DeleteAsync(1, 7);

        _walletManagerMock.Verify(w => w.ReleaseHeldFundsAsync(7, 5m, "Product", 1), Times.Once);
        product.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_WhenPriceIncreases_HoldsAdditionalAmount()
    {
        var product = new Product { Id = 1, SellerId = 7, Price = 100m, Category = new Category { Id = 1, Name = "Test" } };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        await CreateManager().UpdateAsync(1, 7, new UpdateProductRequest("T", "D", "B", ProductCondition.New, 200m, 10, "Loc", 1, ProductStatus.Available));

        // Old hold = 5, new hold = 10, difference = 5
        _walletManagerMock.Verify(w => w.HoldFundsAsync(7, 5m, "Product", 1), Times.Once);
        _walletManagerMock.Verify(w => w.ReleaseHeldFundsAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenPriceDecreases_ReleasesExcessHold()
    {
        var product = new Product { Id = 1, SellerId = 7, Price = 200m, Category = new Category { Id = 1, Name = "Test" } };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        await CreateManager().UpdateAsync(1, 7, new UpdateProductRequest("T", "D", "B", ProductCondition.New, 100m, 10, "Loc", 1, ProductStatus.Available));

        // Old hold = 10, new hold = 5, difference = 5
        _walletManagerMock.Verify(w => w.ReleaseHeldFundsAsync(7, 5m, "Product", 1), Times.Once);
        _walletManagerMock.Verify(w => w.HoldFundsAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RejectProductAsync_ReleasesFivePercentHold()
    {
        var product = new Product { Id = 1, SellerId = 7, Price = 100m, Status = ProductStatus.PendingReview, Category = new Category { Id = 1, Name = "Test" } };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        await CreateManager().RejectProductAsync(1, 99, "Policy violation");

        _walletManagerMock.Verify(w => w.ReleaseHeldFundsAsync(7, 5m, "Product", 1), Times.Once);
    }

}