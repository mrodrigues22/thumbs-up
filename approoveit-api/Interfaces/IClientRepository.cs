using ApprooveItApi.Models;

namespace ApprooveItApi.Repositories;

public interface IClientRepository
{
    Task<IEnumerable<Client>> ListByUserAsync(string userId);
    Task<Client?> GetByIdAsync(Guid id, string userId);
    Task<Client?> FindByEmailAsync(string email, string userId);
    Task<Client> CreateAsync(Client client);
    Task<Client> UpdateAsync(Client client);
    Task DeleteAsync(Guid id);
    Task<int> GetSubmissionCountAsync(Guid clientId);
}
