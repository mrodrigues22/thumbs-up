using ApprooveItApi.Models;

namespace ApprooveItApi.Repositories;

public interface IClientSummaryRepository
{
    Task<ClientSummary?> GetByClientIdAsync(Guid clientId);
    Task<ClientSummary> UpsertAsync(ClientSummary summary); // Creates or updates based on ClientId
    Task<bool> SaveChangesAsync();
}
