using ApprooveItApi.Models;

namespace ApprooveItApi.Helpers;

public class SubmissionQueryObject
{
    public SubmissionStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public string? SortOrder { get; set; } = "desc";
    
    // Pagination properties (optional for future use)
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
