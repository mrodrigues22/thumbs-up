using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApprooveItApi.DTOs;

public class ClientSummaryResponse
{
    public Guid ClientId { get; set; }
    public List<string> StylePreferences { get; set; } = new();
    public List<string> RecurringPositives { get; set; } = new();
    public List<string> RejectionReasons { get; set; } = new();
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public DateTime GeneratedAt { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SummaryDataStatus DataStatus { get; set; } = SummaryDataStatus.PendingAnalysis;
    public List<string> MissingSignals { get; set; } = new();
    public int PendingAnalysisCount { get; set; }
    public int FeatureCoverageCount { get; set; }
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
    public double? Probability { get; set; }
    public string Rationale { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ApprovalPredictionStatus Status { get; set; } = ApprovalPredictionStatus.PendingSignals;
    public string StatusMessage { get; set; } = string.Empty;
}

public enum SummaryDataStatus
{
    PendingAnalysis,
    InsufficientHistory,
    Ready,
    Partial
}

public enum ApprovalPredictionStatus
{
    PendingSignals,
    Ready,
    MissingHistory,
    Error
}
