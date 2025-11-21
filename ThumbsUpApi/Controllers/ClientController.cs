using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly IClientRepository _clientRepository;

    public ClientController(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientResponse>>> GetClients()
    {
        var userId = GetUserId();
        var clients = await _clientRepository.ListByUserAsync(userId);
        
        var clientResponses = new List<ClientResponse>();
        foreach (var client in clients)
        {
            var submissionCount = await _clientRepository.GetSubmissionCountAsync(client.Id);
            clientResponses.Add(new ClientResponse
            {
                Id = client.Id,
                Email = client.Email,
                Name = client.Name,
                CompanyName = client.CompanyName,
                CreatedAt = client.CreatedAt,
                LastUsedAt = client.LastUsedAt,
                SubmissionCount = submissionCount
            });
        }
        
        return Ok(clientResponses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientResponse>> GetClient(Guid id)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        var submissionCount = await _clientRepository.GetSubmissionCountAsync(client.Id);
        
        return Ok(new ClientResponse
        {
            Id = client.Id,
            Email = client.Email,
            Name = client.Name,
            CompanyName = client.CompanyName,
            CreatedAt = client.CreatedAt,
            LastUsedAt = client.LastUsedAt,
            SubmissionCount = submissionCount
        });
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> CreateClient([FromBody] CreateClientRequest request)
    {
        var userId = GetUserId();
        
        // Check if client with this email already exists for this user
        var existingClient = await _clientRepository.FindByEmailAsync(request.Email, userId);
        if (existingClient != null)
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }
        
        var client = new Client
        {
            Id = Guid.NewGuid(),
            CreatedById = userId,
            Email = request.Email,
            Name = request.Name,
            CompanyName = request.CompanyName,
            CreatedAt = DateTime.UtcNow
        };
        
        await _clientRepository.CreateAsync(client);
        
        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, new ClientResponse
        {
            Id = client.Id,
            Email = client.Email,
            Name = client.Name,
            CompanyName = client.CompanyName,
            CreatedAt = client.CreatedAt,
            LastUsedAt = client.LastUsedAt,
            SubmissionCount = 0
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClientResponse>> UpdateClient(Guid id, [FromBody] UpdateClientRequest request)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        // Check if another client with this email already exists for this user
        var existingClient = await _clientRepository.FindByEmailAsync(request.Email, userId);
        if (existingClient != null && existingClient.Id != id)
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }
        
        client.Email = request.Email;
        client.Name = request.Name;
        client.CompanyName = request.CompanyName;
        
        await _clientRepository.UpdateAsync(client);
        
        var submissionCount = await _clientRepository.GetSubmissionCountAsync(client.Id);
        
        return Ok(new ClientResponse
        {
            Id = client.Id,
            Email = client.Email,
            Name = client.Name,
            CompanyName = client.CompanyName,
            CreatedAt = client.CreatedAt,
            LastUsedAt = client.LastUsedAt,
            SubmissionCount = submissionCount
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        // Check if client has submissions
        var submissionCount = await _clientRepository.GetSubmissionCountAsync(id);
        if (submissionCount > 0)
        {
            return BadRequest(new { message = $"Cannot delete client with {submissionCount} submission(s). Please delete submissions first." });
        }
        
        await _clientRepository.DeleteAsync(id);
        
        return NoContent();
    }
}
