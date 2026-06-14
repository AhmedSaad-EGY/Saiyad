using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sayiad.Data.Data;
using Sayiad.Domain.Common;
using Sayiad.Domain.Constants;
using Sayiad.Domain.Dtos.PaymentDtos;

namespace Sayiad.Tests.Managers;

public class PaymentManagerTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<PaymentManager>> _loggerMock = new();
    private readonly Mock<IWalletManager> _walletManagerMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IOptions<AppSettings>> _settingsMock = new();
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();

    private PaymentManager CreateManager()
    {
        _settingsMock.Setup(s => s.Value).Returns(new AppSettings { AdminEmail = "sayiadapp@gmail.com" });
        return new(_paymentRepoMock.Object, _orderRepoMock.Object, _unitOfWorkMock.Object,
            _loggerMock.Object, _walletManagerMock.Object, _userRepoMock.Object,
            _settingsMock.Object, _auctionRepoMock.Object);
    }

    private Payment CreatePayment(int id = 1, OrderStatus status = OrderStatus.Pending)
    {
        var order = new Order
        {
            Id = 1,
            BuyerId = 42,
            TotalPrice = 500m,
            Status = status,
            OrderItems = new HashSet<OrderItem>
            {
                new() { ProductId = 10, SellerId = 7, UnitPrice = 200m, Quantity = 1, Subtotal = 200m },
                new() { ProductId = 11, SellerId = 7, UnitPrice = 300m, Quantity = 1, Subtotal = 300m },
            }
        };
        return new Payment
        {
            Id = id,
            OrderId = 1,
            Order = order,
            Amount = 500m,
            PaymentMethod = "Card",
            PaymentStatus = PaymentStatus.Pending,
        };
    }

    private void SetupConfirmMocks(Payment payment)
    {
        var txMock = new Mock<IDbContextTransaction>();
        txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(default)).ReturnsAsync(txMock.Object);
        _paymentRepoMock.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
        _orderRepoMock.Setup(r => r.GetByIdAsync(payment.OrderId, 42)).ReturnsAsync(payment.Order);
    }

    [Fact]
    public async Task ConfirmAsync_WithValidPayment_ReleasesProductHolds()
    {
        var payment = CreatePayment();
        SetupConfirmMocks(payment);
        _userRepoMock.Setup(r => r.GetByEmailAsync("sayiadapp@gmail.com")).ReturnsAsync(new User { Id = 1 });

        var result = await CreateManager().ConfirmAsync(payment.Id, 42);

        _walletManagerMock.Verify(w => w.DeductForOrderAsync(42, 500m, 1), Times.Once);
        _walletManagerMock.Verify(w => w.CreditSellerAsync(7, 500m, 1), Times.Once);
        _walletManagerMock.Verify(w => w.CreditPlatformFeeAsync(1, 25m, "Order", 1), Times.Once);
        result.PaymentStatus.Should().Be("Confirmed");
    }

    [Fact]
    public async Task ConfirmAsync_PaymentNotFound_ThrowsKeyNotFoundException()
    {
        _paymentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Payment?)null);
        var act = () => CreateManager().ConfirmAsync(999, 42);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ConfirmAsync_WrongUser_ThrowsUnauthorizedAccessException()
    {
        var payment = CreatePayment();
        _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        var act = () => CreateManager().ConfirmAsync(1, 99);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ConfirmAsync_NotPendingStatus_ThrowsInvalidOperationException()
    {
        var payment = CreatePayment();
        payment.PaymentStatus = PaymentStatus.Confirmed;
        _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        var act = () => CreateManager().ConfirmAsync(1, 42);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not Pending*");
    }

    [Fact]
    public async Task ConfirmAsync_AdminNotFound_SkipsPlatformFeeButReleasesHolds()
    {
        var payment = CreatePayment();
        SetupConfirmMocks(payment);
        _userRepoMock.Setup(r => r.GetByEmailAsync("sayiadapp@gmail.com")).ReturnsAsync((User?)null);

        await CreateManager().ConfirmAsync(payment.Id, 42);

        _walletManagerMock.Verify(w => w.CreditPlatformFeeAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_MultipleSellers_ReleasesHoldsPerSeller()
    {
        var order = new Order
        {
            Id = 3,
            BuyerId = 42,
            TotalPrice = 1000m,
            Status = OrderStatus.Pending,
            OrderItems = new HashSet<OrderItem>
            {
                new() { ProductId = 10, SellerId = 7, UnitPrice = 200m, Quantity = 1, Subtotal = 200m },
                new() { ProductId = 11, SellerId = 8, UnitPrice = 800m, Quantity = 1, Subtotal = 800m },
            }
        };
        var payment = new Payment
        {
            Id = 3,
            OrderId = 3,
            Order = order,
            Amount = 1000m,
            PaymentMethod = "Card",
            PaymentStatus = PaymentStatus.Pending,
        };
        SetupConfirmMocks(payment);
        _userRepoMock.Setup(r => r.GetByEmailAsync("sayiadapp@gmail.com")).ReturnsAsync(new User { Id = 1 });

        await CreateManager().ConfirmAsync(payment.Id, 42);

        _walletManagerMock.Verify(w => w.CreditSellerAsync(7, 200m, 3), Times.Once);
        _walletManagerMock.Verify(w => w.CreditSellerAsync(8, 800m, 3), Times.Once);
    }
}
