using System.Security.Claims;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Middleware;

public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionMiddleware> _logger;

    public SubscriptionMiddleware(RequestDelegate next, ILogger<SubscriptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService)
    {
        // Skip middleware for non-authenticated requests
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Skip middleware for subscription-related endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/subscription") || 
            path.Contains("/paddle") ||
            path.Contains("/auth") ||
            path.Contains("/swagger"))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        try
        {
            // Get user's subscription
            var subscription = await subscriptionService.GetUserSubscriptionAsync(userId);
            
            // Block users without any subscription
            if (subscription == null)
            {
                _logger.LogWarning("User {UserId} has no subscription - blocking access", userId);
                context.Response.StatusCode = 402; // Payment Required
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Please subscribe to a plan to access Thumbs Up features.",
                    requiresSubscription = true,
                    pricingUrl = "/pricing"
                });
                return;
            }

            // Check if subscription is inactive
            if (subscription.Status == SubscriptionStatus.Expired || 
                subscription.Status == SubscriptionStatus.PastDue ||
                subscription.Status == SubscriptionStatus.Cancelled)
            {
                _logger.LogWarning("User {UserId} has inactive subscription - blocking access", userId);
                context.Response.StatusCode = 402; // Payment Required
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Your subscription is inactive. Please renew your subscription.",
                    status = subscription.Status.ToString(),
                    subscriptionUrl = "/subscription"
                });
                return;
            }

            // Add subscription info to request context for use in controllers
            context.Items["SubscriptionTier"] = subscription.Tier;
            context.Items["Subscription"] = subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in subscription middleware for user {UserId}", userId);
            // Continue processing even if subscription check fails
        }

        await _next(context);
    }
}

public static class SubscriptionMiddlewareExtensions
{
    public static IApplicationBuilder UseSubscriptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SubscriptionMiddleware>();
    }
}
