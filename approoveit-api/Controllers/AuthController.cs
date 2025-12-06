using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ApprooveItApi.DTOs;
using ApprooveItApi.Models;
using ApprooveItApi.Repositories;
using ApprooveItApi.Services;

namespace ApprooveItApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageCompressionService _imageCompressionService;
    
    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IUserRepository userRepository,
        IFileStorageService fileStorageService,
        IImageCompressionService imageCompressionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _imageCompressionService = imageCompressionService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName
        };
        
        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        _logger.LogInformation("User {Email} registered successfully", request.Email);
        
        // Auto login after registration
        var token = GenerateJwtToken(user);
        
        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours())
        });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }
        
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }
        
        var token = GenerateJwtToken(user);
        
        _logger.LogInformation("User {Email} logged in successfully", request.Email);
        
        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours())
        });
    }
    
    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private int GetTokenExpirationHours()
    {
        return _configuration.GetValue<int>("Jwt:ExpirationHours", 24);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Update user properties
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.CompanyName = request.CompanyName;

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} updated their profile", userId);

        return Ok(new UserProfileResponse
        {
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    [Authorize]
    [HttpPost("profile/picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { message = "Invalid file type. Only images are allowed." });

        // Validate file size (max 10MB before compression)
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds 10MB limit" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        try
        {
            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                await _fileStorageService.DeleteAsync(user.ProfilePictureUrl);
            }

            // Compress the image to WebP format (800x800 max, 85% quality)
            using var originalStream = file.OpenReadStream();
            using var compressedStream = await _imageCompressionService.CompressImageAsync(originalStream, 800, 800, 85);

            // Generate unique filename with .webp extension
            var uniqueFileName = $"{Guid.NewGuid()}.webp";

            // Upload compressed image
            var filePath = await _fileStorageService.UploadFromStreamAsync(compressedStream, uniqueFileName, "profile-pictures");
            var fileUrl = _fileStorageService.GetFileUrl(filePath);

            // Update user's profile picture URL
            user.ProfilePictureUrl = fileUrl;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} updated their profile picture (compressed to WebP)", userId);

            return Ok(new { profilePictureUrl = fileUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing profile picture for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to process image" });
        }
    }
}
