using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly IPaddleService _paddleService;
    private readonly ISubscriptionLimitService _limitService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IPaddleService paddleService,
        ISubscriptionLimitService limitService,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<SubscriptionController> logger)
    {
        _paddleService = paddleService;
        _limitService = limitService;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    [HttpGet("status")]
    public async Task<ActionResult<SubscriptionResponse>> GetSubscriptionStatus()
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var submissionsLimit = _limitService.GetSubmissionLimit(user.SubscriptionTier);
        var storageLimit = _limitService.GetStorageLimitBytes(user.SubscriptionTier);

        return Ok(new SubscriptionResponse
        {
            Status = user.SubscriptionStatus.ToString(),
            Tier = user.SubscriptionTier.ToString(),
            PaddleSubscriptionId = user.PaddleSubscriptionId,
            StartDate = user.SubscriptionStartDate,
            EndDate = user.SubscriptionEndDate,
            CancelledAt = user.SubscriptionCancelledAt,
            Usage = new UsageStatsResponse
            {
                SubmissionsUsed = user.SubmissionsUsedThisMonth,
                SubmissionsLimit = submissionsLimit,
                StorageUsedBytes = user.StorageUsedBytes,
                StorageLimitBytes = storageLimit,
                StorageUsedFormatted = FormatBytes(user.StorageUsedBytes),
                StorageLimitFormatted = FormatBytes(storageLimit),
                SubmissionsPercentage = submissionsLimit > 0 ? (int)((user.SubmissionsUsedThisMonth / (double)submissionsLimit) * 100) : 0,
                StoragePercentage = (int)((user.StorageUsedBytes / (double)storageLimit) * 100)
            }
        });
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> CreateCheckout([FromBody] CreateCheckoutRequest request)
    {
        try
        {
            var userId = GetUserId();
            var successUrl = request.SuccessUrl ?? $"{_configuration["Frontend:BaseUrl"]}/subscription-success";
            
            var checkoutResponse = await _paddleService.CreateCheckoutSessionAsync(
                request.PriceId, 
                userId, 
                successUrl);

            return Ok(checkoutResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session");
            return StatusCode(500, new { message = "Failed to create checkout session" });
        }
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (string.IsNullOrEmpty(user.PaddleSubscriptionId))
                return BadRequest(new { message = "No active subscription found" });

            await _paddleService.CancelSubscriptionAsync(user.PaddleSubscriptionId, request.Immediately);

            if (request.Immediately)
            {
                user.SubscriptionStatus = SubscriptionStatus.Cancelled;
                user.SubscriptionCancelledAt = DateTime.UtcNow;
            }
            else
            {
                user.SubscriptionCancelledAt = user.SubscriptionEndDate;
            }

            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Subscription cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription");
            return StatusCode(500, new { message = "Failed to cancel subscription" });
        }
    }

    [HttpPost("reactivate")]
    public async Task<IActionResult> ReactivateSubscription()
    {
        try
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (string.IsNullOrEmpty(user.PaddleSubscriptionId))
                return BadRequest(new { message = "No subscription found" });

            if (user.SubscriptionStatus != SubscriptionStatus.Cancelled)
                return BadRequest(new { message = "Subscription is not cancelled" });

            // Remove cancellation
            user.SubscriptionCancelledAt = null;
            user.SubscriptionStatus = SubscriptionStatus.Active;

            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Subscription reactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reactivate subscription");
            return StatusCode(500, new { message = "Failed to reactivate subscription" });
        }
    }

    [HttpPost("upgrade")]
    public async Task<IActionResult> UpgradeSubscription([FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (string.IsNullOrEmpty(user.PaddleSubscriptionId))
                return BadRequest(new { message = "No active subscription found" });

            await _paddleService.UpdateSubscriptionAsync(user.PaddleSubscriptionId, request.NewPriceId);

            return Ok(new { message = "Subscription updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade subscription");
            return StatusCode(500, new { message = "Failed to upgrade subscription" });
        }
    }

    [HttpGet("payment-method-url")]
    public async Task<ActionResult<string>> GetPaymentMethodUrl()
    {
        try
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (string.IsNullOrEmpty(user.PaddleSubscriptionId))
                return BadRequest(new { message = "No active subscription found" });

            var url = await _paddleService.GetUpdatePaymentMethodUrlAsync(user.PaddleSubscriptionId);

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment method URL");
            return StatusCode(500, new { message = "Failed to get payment method URL" });
        }
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public ActionResult<IEnumerable<SubscriptionPlanResponse>> GetPlans()
    {
        var plans = new List<SubscriptionPlanResponse>
        {
            new SubscriptionPlanResponse
            {
                Tier = "Starter",
                PriceId = _configuration["Paddle:PriceIds:Starter"] ?? "",
                Price = 9.00m,
                Currency = "USD",
                Interval = "month",
                SubmissionsLimit = 20,
                ClientsLimit = 10,
                StorageLimitBytes = 1L * 1024 * 1024 * 1024, // 1 GB
                HasAiFeatures = false,
                HasCustomBranding = false
            },
            new SubscriptionPlanResponse
            {
                Tier = "Pro",
                PriceId = _configuration["Paddle:PriceIds:Pro"] ?? "",
                Price = 19.00m,
                Currency = "USD",
                Interval = "month",
                SubmissionsLimit = -1, // Unlimited
                ClientsLimit = -1, // Unlimited
                StorageLimitBytes = 10L * 1024 * 1024 * 1024, // 10 GB
                HasAiFeatures = true,
                HasCustomBranding = true
            },
            new SubscriptionPlanResponse
            {
                Tier = "Enterprise",
                PriceId = _configuration["Paddle:PriceIds:Enterprise"] ?? "",
                Price = 99.00m,
                Currency = "USD",
                Interval = "month",
                SubmissionsLimit = -1, // Unlimited
                ClientsLimit = -1, // Unlimited
                StorageLimitBytes = 100L * 1024 * 1024 * 1024, // 100 GB
                HasAiFeatures = true,
                HasCustomBranding = true
            }
        };

        return Ok(plans);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
