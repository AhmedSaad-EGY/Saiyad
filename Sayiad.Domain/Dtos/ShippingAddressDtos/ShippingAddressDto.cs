namespace Sayiad.Domain.Dtos.ShippingAddressDtos;

public record CreateShippingAddressRequest(
    string FullName,
    string Phone,
    string City,
    string AddressLine,
    string? PostalCode
);

public record ShippingAddressResponse(
    int Id,
    string FullName,
    string Phone,
    string City,
    string AddressLine,
    string? PostalCode,
    bool IsDefault,
    DateTime CreatedAt
);
