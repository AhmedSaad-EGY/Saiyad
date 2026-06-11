namespace Sayiad.Data.Repository.SellerProfileRepo;

public interface ISellerProfileRepository
{
    Task<SellerProfile?> GetByIdAsync(int id);
    Task<SellerProfile?> GetByUserIdAsync(int userId);
    Task<SellerProfile> CreateAsync(SellerProfile profile);
    Task<SellerProfile> UpdateAsync(SellerProfile profile);
    Task<bool> DeleteAsync(int id, int userId);
    Task UpdateRatingAsync(int sellerId);
    Task IncrementSalesAsync(int sellerId);
}
