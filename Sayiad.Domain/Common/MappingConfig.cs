using Mapster;
using Sayiad.Domain.Dtos.AuctionDtos;
using Sayiad.Domain.Dtos.CategoryDtos;
using Sayiad.Domain.Dtos.NotificationDtos;
using Sayiad.Domain.Dtos.OrderDtos;
using Sayiad.Domain.Dtos.ProductDtos;
using Sayiad.Domain.Dtos.ReviewDtos;
using Sayiad.Domain.Dtos.SellerProfileDtos;
using Sayiad.Domain.Dtos.ShippingAddressDtos;

namespace Sayiad.Domain.Common;

public static class MappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<Product, ProductResponse>.NewConfig()
            .Map(dest => dest.SellerName, src => src.Seller.FullName)
            .Map(dest => dest.CategoryName, src => src.Category.Name)
            .Map(dest => dest.PrimaryImageUrl,
                src => src.Images.FirstOrDefault(i => i.IsPrimary)!.ImageUrl);

        TypeAdapterConfig<CustomerOrder, OrderResponse>.NewConfig()
            .Map(dest => dest.BuyerName, src => src.Buyer.FullName);

        TypeAdapterConfig<Review, ReviewResponse>.NewConfig()
            .Map(dest => dest.UserName, src => src.User.FullName);

        TypeAdapterConfig<Category, CategoryResponse>.NewConfig();

        TypeAdapterConfig<Notification, NotificationResponse>.NewConfig();

        TypeAdapterConfig<SellerProfile, SellerProfileResponse>.NewConfig()
            .Map(dest => dest.SellerName, src => src.User.FullName);

        TypeAdapterConfig<ShippingAddress, ShippingAddressResponse>.NewConfig();

        TypeAdapterConfig<Auction, AuctionResponse>.NewConfig()
            .Map(dest => dest.ProductTitle, src => src.Product!.Title)
            .Map(dest => dest.WinnerName, src =>
                src.Winner != null ? src.Winner.FullName : null);
    }
}
