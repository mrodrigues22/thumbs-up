using Microsoft.EntityFrameworkCore;
using ApprooveItApi.Data;
using ApprooveItApi.Models;

namespace ApprooveItApi.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;

    public ClientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Client>> ListByUserAsync(string userId)
    {
        return await _context.Clients
            .Where(c => c.CreatedById == userId)
            .OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(Guid id, string userId)
    {
        return await _context.Clients
            .Where(c => c.Id == id && c.CreatedById == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<Client?> FindByEmailAsync(string email, string userId)
    {
        return await _context.Clients
            .Where(c => c.Email == email && c.CreatedById == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<Client> CreateAsync(Client client)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<Client> UpdateAsync(Client client)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetSubmissionCountAsync(Guid clientId)
    {
        return await _context.Submissions
            .Where(s => s.ClientId == clientId)
            .CountAsync();
    }
}
