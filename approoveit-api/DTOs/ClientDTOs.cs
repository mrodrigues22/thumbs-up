using System.ComponentModel.DataAnnotations;

namespace ApprooveItApi.DTOs;

public class ClientResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int SubmissionCount { get; set; }
}

public class CreateClientRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? CompanyName { get; set; }
}

public class UpdateClientRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? Name { get; set; }
    
    public string? CompanyName { get; set; }
}
