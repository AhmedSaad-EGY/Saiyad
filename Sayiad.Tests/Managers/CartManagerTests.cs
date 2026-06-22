using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sayiad.Data.Data;

namespace Sayiad.Tests.Managers;

public class CartManagerTests
{
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CartManager>> _loggerMock = new();

    private CartManager CreateManager() =>
        new(_cartRepoMock.Object, _productRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task AddItem_WhenProductIsAuctioned_ThrowsInvalidOperationException()
    {
        var product = new Product
        {
            Id = 1, Title = "Auction Item", Price = 100m, StockQuantity = 1,
            Status = ProductStatus.Available, IsAuctioned = true
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        var act = () => CreateManager().AddItemAsync(42, new AddToCartRequest(1, 1));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Auction items cannot be purchased directly. Please bid through the auction.");
    }

    [Fact]
    public async Task AddItem_WhenProductIsNotAuctionedAndAvailable_Succeeds()
    {
        var product = new Product
        {
            Id = 1, Title = "Normal Item", Price = 50m, StockQuantity = 10,
            Status = ProductStatus.Available, IsAuctioned = false
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _cartRepoMock.Setup(r => r.GetCartAsync(42)).ReturnsAsync((Cart?)null);
        _cartRepoMock.Setup(r => r.AddAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);

        var result = await CreateManager().AddItemAsync(42, new AddToCartRequest(1, 2));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AddItem_WhenProductIsAuctioned_DoesNotCallSaveChanges()
    {
        var product = new Product
        {
            Id = 1, Title = "Auction Item", Price = 100m, StockQuantity = 1,
            Status = ProductStatus.Available, IsAuctioned = true
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        var act = () => CreateManager().AddItemAsync(42, new AddToCartRequest(1, 1));
        await act.Should().ThrowAsync<InvalidOperationException>();

        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
