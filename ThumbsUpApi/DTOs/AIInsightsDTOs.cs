using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.DTOs;

public class ClientSummaryResponse
{
    public Guid ClientId { get; set; }
    public string Summary { get; set; } = string.Empty; // counts signature stripped
    public DateTime GeneratedAt { get; set; }
}

public class ApprovalPredictionRequest
{
    [Required]
    public Guid ClientId { get; set; }
    [Required]
    public Guid SubmissionId { get; set; }
}

public class ApprovalPredictionResponse
{
    public Guid ClientId { get; set; }
    public Guid SubmissionId { get; set; }
    public double Probability { get; set; }
    public string Rationale { get; set; } = string.Empty;
}
