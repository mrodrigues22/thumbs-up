using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Configuration;

public sealed class AiPredictorOptions
{
    /// <summary>
    /// Weight applied to matching tags when calculating approval probability.
    /// Default is 0.05 (5% boost per matching tag).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "TagWeight must be between 0.0 and 1.0")]
    public double TagWeight { get; set; } = 0.05;

    /// <summary>
    /// Weight applied when a submission aligns with documented client summary highlights.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "SummaryAlignmentWeight must be between 0.0 and 1.0")]
    public double SummaryAlignmentWeight { get; set; } = 0.04;

    /// <summary>
    /// Weight applied as a penalty when a submission touches on known rejection triggers.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "SummaryPenaltyWeight must be between 0.0 and 1.0")]
    public double SummaryPenaltyWeight { get; set; } = 0.06;
}
