using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ITransactionRepository transactionRepository,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's subscription details
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionResponse>> GetCurrentSubscription()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
            if (subscription == null)
                return NotFound(new { message = "Subscription not found" });

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription");
            return StatusCode(500, new { message = "Error retrieving subscription" });
        }
    }

    /// <summary>
    /// Get current user's usage statistics
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<UsageStats>> GetUsage()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var usage = await _subscriptionService.GetUsageStatsAsync(userId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage stats");
            return StatusCode(500, new { message = "Error retrieving usage statistics" });
        }
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PlanResponse>>> GetAvailablePlans()
    {
        try
        {
            var plans = await _subscriptionService.GetAvailablePlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving plans");
            return StatusCode(500, new { message = "Error retrieving plans" });
        }
    }

    /// <summary>
    /// Create checkout session for upgrading subscription
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized checkout attempt");
                return Unauthorized(new { message = "You must be logged in to subscribe" });
            }

            if (request == null || string.IsNullOrEmpty(request.PriceId))
            {
                _logger.LogWarning("Invalid checkout request: missing price ID");
                return BadRequest(new { message = "Invalid request: price ID is required" });
            }

            var checkoutSession = await _subscriptionService.CreateCheckoutAsync(userId, request);
            return Ok(checkoutSession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session: {Message}", ex.Message);
            
            // Return the actual error message to help with debugging
            var message = ex.InnerException?.Message ?? ex.Message;
            
            // Check if it's a Paddle-specific error
            if (message.Contains("Paddle API"))
            {
                return StatusCode(503, new { 
                    message = "Payment service is currently unavailable. Please try again later.",
                    details = message 
                });
            }
            
            return StatusCode(500, new { 
                message = "Error creating checkout session. Please try again.",
                details = message
            });
        }
    }

    /// <summary>
    /// Cancel current subscription
    /// </summary>
    [HttpPost("cancel")]
    public async Task<ActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var result = await _subscriptionService.CancelSubscriptionAsync(userId, request);
            
            if (!result)
                return BadRequest(new { message = "Failed to cancel subscription" });

            return Ok(new { message = "Subscription cancellation initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription");
            return StatusCode(500, new { message = "Error cancelling subscription" });
        }
    }

    /// <summary>
    /// Get customer portal URL for managing subscription
    /// </summary>
    [HttpPost("customer-portal")]
    public async Task<ActionResult<object>> GetCustomerPortalUrl()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var portalUrl = await _subscriptionService.GetCustomerPortalUrlAsync(userId);
            return Ok(new { url = portalUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer portal URL");
            return StatusCode(500, new { message = "Error accessing customer portal" });
        }
    }

    /// <summary>
    /// Get transaction history
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionResponse>>> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var transactions = await _transactionRepository.GetByUserIdAsync(userId, pageSize, page);
            
            var response = transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                PaddleTransactionId = t.PaddleTransactionId,
                Amount = t.Amount,
                Currency = t.Currency,
                Status = t.Status,
                Type = t.Type,
                Details = t.Details,
                CreatedAt = t.CreatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return StatusCode(500, new { message = "Error retrieving transactions" });
        }
    }

    /// <summary>
    /// Check if user has access to a specific feature
    /// </summary>
    [HttpGet("feature/{featureName}")]
    public async Task<ActionResult<object>> CheckFeatureAccess(string featureName)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var hasAccess = await _subscriptionService.CheckFeatureAccessAsync(userId, featureName);
            return Ok(new { feature = featureName, hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access");
            return StatusCode(500, new { message = "Error checking feature access" });
        }
    }
}
