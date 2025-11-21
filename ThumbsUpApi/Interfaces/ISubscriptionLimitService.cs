using ThumbsUpApi.Models;

namespace ThumbsUpApi.Interfaces;

public interface ISubscriptionLimitService
{
    Task<bool> CanCreateSubmissionAsync(string userId);
    Task<bool> CanUploadFileAsync(string userId, long fileSizeBytes);
    Task IncrementSubmissionCountAsync(string userId);
    Task IncrementStorageUsageAsync(string userId, long bytes);
    Task DecrementStorageUsageAsync(string userId, long bytes);
    Task ResetMonthlyUsageAsync(string userId);
    bool HasAiFeatures(SubscriptionTier tier);
    int GetSubmissionLimit(SubscriptionTier tier);
    long GetStorageLimitBytes(SubscriptionTier tier);
}
