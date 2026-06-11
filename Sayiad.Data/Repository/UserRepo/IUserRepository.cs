namespace Sayiad.Data.Repository.UserRepo;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<PagedResult<User>> GetAllAsync(PaginationRequest pagination);
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByPreviousRefreshTokenHashAsync(string previousHash);
    Task<User?> GetByVerificationTokenAsync(string token);
}
