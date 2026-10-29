using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AIStudyAssistant.API.Data;
using AIStudyAssistant.API.Data.Entities;
using AIStudyAssistant.API.DTOs.Auth;

namespace AIStudyAssistant.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            _logger.LogWarning("Registration failed: Email '{Email}' is already registered.", normalizedEmail);
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new User
        {
            DisplayName = request.DisplayName.Trim(),
            Email = normalizedEmail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully registered new user: {UserId}", user.Id);

        var (token, expiresAt) = GenerateJwtToken(user);

        return new AuthResponse(
            token,
            MapToDto(user),
            expiresAt
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: No user found for email '{Email}'.", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: Password verification failed for user '{UserId}'.", user.Id);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var (token, expiresAt) = GenerateJwtToken(user);

        return new AuthResponse(
            token,
            MapToDto(user),
            expiresAt
        );
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return MapToDto(user);
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? "A_Super_Secret_Key_For_AIStudyAssistant_Development_Must_Be_At_Least_32_Chars_Long!";

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters long.");
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "AIStudyAssistant.API";
        var audience = _configuration["Jwt:Audience"] ?? "AIStudyAssistant.Client";
        var expiryDays = int.TryParse(_configuration["Jwt:ExpiryDays"], out var days) ? days : 7;
        var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return (token, expiresAt);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        Email = user.Email,
        CreatedAt = user.CreatedAt
    };
}
