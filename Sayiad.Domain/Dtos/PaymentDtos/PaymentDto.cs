namespace Sayiad.Domain.Dtos.PaymentDtos;

public record TransactionResponse(int Id, string Reference, decimal Amount, string Status, DateTime CreatedAt);
public record PaymentResponse(int Id, int OrderId, decimal Amount, string PaymentMethod, string PaymentStatus, DateTime? PaidAt, DateTime CreatedAt, List<TransactionResponse> Transactions);
public record InitiatePaymentRequest(int OrderId, string PaymentMethod);
