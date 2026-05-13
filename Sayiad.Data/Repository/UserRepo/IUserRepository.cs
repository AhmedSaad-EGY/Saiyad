namespace Sayiad.Data.Repository.UserRepo;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<PagedResult<User>> GetAllAsync(PaginationRequest pagination);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByVerificationTokenAsync(string token);
}
