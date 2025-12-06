namespace ApprooveItApi.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends a review link to the client
    /// </summary>
    Task SendReviewLinkAsync(string clientEmail, string reviewLink, string accessPassword, string? message = null);
    
    /// <summary>
    /// Sends a notification to the professional when a review is submitted
    /// </summary>
    Task SendReviewNotificationAsync(string professionalEmail, string submissionId, bool isApproved);
}
