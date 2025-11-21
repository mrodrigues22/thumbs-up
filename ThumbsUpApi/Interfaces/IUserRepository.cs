using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string userId);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<IEnumerable<ApplicationUser>> GetAllAsync();
    Task<ApplicationUser?> UpdateAsync(ApplicationUser user);
}
