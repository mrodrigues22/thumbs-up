using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;
using ThumbsUpApi.Mappers;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly SubmissionMapper _submissionMapper;

    public ClientController(
        IClientRepository clientRepository,
        ISubmissionRepository submissionRepository,
        SubmissionMapper submissionMapper)
    {
        _clientRepository = clientRepository;
        _submissionRepository = submissionRepository;
        _submissionMapper = submissionMapper;
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

    [HttpGet("{id}/submissions")]
    public async Task<ActionResult<IEnumerable<SubmissionResponse>>> GetClientSubmissions(Guid id)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        var submissions = await _submissionRepository.GetByClientIdAsync(id, userId);
        
        return Ok(submissions.Select(s => _submissionMapper.ToResponse(s)));
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
