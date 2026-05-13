using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Data;
using Sayiad.Data.Repository.SellerProfileRepo;

namespace Sayiad.Data.Repository.SellerProfileRepo;

public class SellerProfileRepository : ISellerProfileRepository
{
    private readonly ApplicationDbContext _db;

    public SellerProfileRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SellerProfile?> GetByIdAsync(int id)
    {
        return await _db.SellerProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SellerProfile?> GetByUserIdAsync(int userId)
    {
        return await _db.SellerProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<SellerProfile> CreateAsync(SellerProfile profile)
    {
        _db.SellerProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task<SellerProfile> UpdateAsync(SellerProfile profile)
    {
        _db.SellerProfiles.Update(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var profile = await _db.SellerProfiles.FindAsync(id);
        if (profile == null) return false;

        _db.SellerProfiles.Remove(profile);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateRatingAsync(int sellerId)
    {
        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == sellerId);
        if (profile == null) return;

        var avg = await _db.Reviews
            .Where(r => r.Product.SellerId == sellerId)
            .AverageAsync(r => (decimal?)r.Rating) ?? 0;

        profile.AverageRating = avg;
        await _db.SaveChangesAsync();
    }

    public async Task IncrementSalesAsync(int sellerId)
    {
        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == sellerId);
        if (profile == null) return;

        profile.TotalSales++;
        await _db.SaveChangesAsync();
    }
}
