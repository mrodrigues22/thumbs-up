namespace ThumbsUpApi.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;
    
    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }
    
    public Task SendReviewLinkAsync(string clientEmail, string reviewLink, string accessPassword, string? message = null)
    {
        _logger.LogInformation("""
            ============================================
            MOCK EMAIL - Review Link
            ============================================
            To: {ClientEmail}
            Subject: Review Request - Your Approval Needed
            
            Hello!
            
            You've received media content for your review and approval.
            
            {Message}
            
            Review Link: {ReviewLink}
            Access Password: {AccessPassword}
            
            This link will expire in 7 days.
            
            ============================================
            """, clientEmail, message ?? "", reviewLink, accessPassword);
        
        return Task.CompletedTask;
    }
    
    public Task SendReviewNotificationAsync(string professionalEmail, string submissionId, bool isApproved)
    {
        var status = isApproved ? "APPROVED" : "REJECTED";
        
        _logger.LogInformation("""
            ============================================
            MOCK EMAIL - Review Notification
            ============================================
            To: {ProfessionalEmail}
            Subject: Submission {Status}
            
            Your submission (ID: {SubmissionId}) has been {Status} by the client.
            
            Log in to your dashboard to view details.
            
            ============================================
            """, professionalEmail, status, submissionId, status);
        
        return Task.CompletedTask;
    }
}
