using Sayiad.Domain.Dtos.PaymentDtos;

namespace Sayiad.Domain.Managers;

public interface IPaymentManager
{
    Task<PaymentResponse> InitiateAsync(int userId, InitiatePaymentRequest request);
    Task<PaymentResponse> ConfirmAsync(int paymentId, int userId);
    Task<IEnumerable<PaymentResponse>> GetOrderPaymentsAsync(int orderId, int userId);
}
