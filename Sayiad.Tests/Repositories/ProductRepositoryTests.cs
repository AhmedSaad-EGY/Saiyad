using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Data;
using Sayiad.Data.Models;

namespace Sayiad.Tests.Repositories;

public class ProductRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ByDefault_ExcludesAuctionProducts()
    {
        await using var context = CreateContext();
        SeedProducts(context);
        var repo = new ProductRepository(context);

        var result = await repo.GetAllAsync(
            new ProductFilterRequest(null, null, null, null, null, null),
            new PaginationRequest { Page = 1, PageSize = 50 });

        result.Items.Should().NotContain(p => p.IsAuctioned);
    }

    [Fact]
    public async Task GetAllAsync_WithIsAuctionedFalse_StillExcludesAuctionProducts()
    {
        await using var context = CreateContext();
        SeedProducts(context);
        var repo = new ProductRepository(context);

        var result = await repo.GetAllAsync(
            new ProductFilterRequest(null, null, null, null, null, null, IsAuctioned: false),
            new PaginationRequest { Page = 1, PageSize = 50 });

        result.Items.Should().NotContain(p => p.IsAuctioned);
    }

    [Fact]
    public async Task GetAllAsync_IncludesNonAuctionAvailableProducts()
    {
        await using var context = CreateContext();
        SeedProducts(context);
        var repo = new ProductRepository(context);

        var result = await repo.GetAllAsync(
            new ProductFilterRequest(null, null, null, null, null, null),
            new PaginationRequest { Page = 1, PageSize = 50 });

        result.Items.Should().Contain(p => p.Title == "Normal Product");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedProducts(ApplicationDbContext context)
    {
        var seller = new User
        {
            FullName = "Seller", Email = "seller@test.com",
            PasswordHash = "hash", Phone = "123",
            Role = UserRole.Auctioneer, IsActive = true,
            IsEmailVerified = true, CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(seller);
        var cat = new Category { Name = "TestCat", CreatedAt = DateTime.UtcNow };
        context.Categories.Add(cat);
        context.SaveChanges();

        context.Products.Add(new Product
        {
            Title = "Normal Product", Description = "Regular item",
            Brand = "B", Location = "L", Price = 50m, StockQuantity = 5,
            Status = ProductStatus.Available, IsAuctioned = false,
            Condition = ProductCondition.New, CategoryId = cat.Id,
            SellerId = seller.Id, CreatedAt = DateTime.UtcNow
        });

        context.Products.Add(new Product
        {
            Title = "Auction Product", Description = "Auction-only item",
            Brand = "B", Location = "L", Price = 100m, StockQuantity = 1,
            Status = ProductStatus.Available, IsAuctioned = true,
            Condition = ProductCondition.New, CategoryId = cat.Id,
            SellerId = seller.Id, CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();
        context.ChangeTracker.Clear();
    }
}
