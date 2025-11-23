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
}
