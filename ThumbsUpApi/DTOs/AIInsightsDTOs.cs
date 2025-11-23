using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.DTOs;

public class ClientSummaryResponse
{
    public Guid ClientId { get; set; }
    public List<string> StylePreferences { get; set; } = new();
    public List<string> RecurringPositives { get; set; } = new();
    public List<string> RejectionReasons { get; set; } = new();
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
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
