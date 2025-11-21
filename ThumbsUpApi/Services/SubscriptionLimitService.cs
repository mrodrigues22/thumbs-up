using Microsoft.AspNetCore.Identity;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Services;

public class SubscriptionLimitService : ISubscriptionLimitService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SubscriptionLimitService> _logger;

    public SubscriptionLimitService(
        UserManager<ApplicationUser> userManager,
        ILogger<SubscriptionLimitService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> CanCreateSubmissionAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var limit = GetSubmissionLimit(user.SubscriptionTier);
        
        // Unlimited for Pro and Enterprise
        if (limit == -1) return true;

        // Check if user has reached their limit
        return user.SubmissionsUsedThisMonth < limit;
    }

    public async Task<bool> CanUploadFileAsync(string userId, long fileSizeBytes)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var limit = GetStorageLimitBytes(user.SubscriptionTier);
        
        // Check if adding this file would exceed the limit
        return (user.StorageUsedBytes + fileSizeBytes) <= limit;
    }

    public async Task IncrementSubmissionCountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.SubmissionsUsedThisMonth++;
        await _userManager.UpdateAsync(user);
        
        _logger.LogInformation("Incremented submission count for user {UserId} to {Count}", 
            userId, user.SubmissionsUsedThisMonth);
    }

    public async Task IncrementStorageUsageAsync(string userId, long bytes)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.StorageUsedBytes += bytes;
        await _userManager.UpdateAsync(user);
        
        _logger.LogInformation("Incremented storage usage for user {UserId} by {Bytes} bytes", 
            userId, bytes);
    }

    public async Task DecrementStorageUsageAsync(string userId, long bytes)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.StorageUsedBytes = Math.Max(0, user.StorageUsedBytes - bytes);
        await _userManager.UpdateAsync(user);
        
        _logger.LogInformation("Decremented storage usage for user {UserId} by {Bytes} bytes", 
            userId, bytes);
    }

    public async Task ResetMonthlyUsageAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.SubmissionsUsedThisMonth = 0;
        user.UsageResetDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        _logger.LogInformation("Reset monthly usage for user {UserId}", userId);
    }

    public bool HasAiFeatures(SubscriptionTier tier)
    {
        return tier >= SubscriptionTier.Pro;
    }

    public int GetSubmissionLimit(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.None => 0, // No submissions without subscription
            SubscriptionTier.Starter => 20,
            SubscriptionTier.Pro => -1, // Unlimited
            SubscriptionTier.Enterprise => -1, // Unlimited
            _ => 0
        };
    }

    public long GetStorageLimitBytes(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.None => 0, // No storage without subscription
            SubscriptionTier.Starter => 1L * 1024 * 1024 * 1024, // 1 GB
            SubscriptionTier.Pro => 10L * 1024 * 1024 * 1024, // 10 GB
            SubscriptionTier.Enterprise => 100L * 1024 * 1024 * 1024, // 100 GB
            _ => 0
        };
    }
}
