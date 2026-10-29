using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIStudyAssistant.API.DTOs.Auth;
using AIStudyAssistant.API.Services;

namespace AIStudyAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public const string AuthCookieName = "X-Access-Token";

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user account and returns an authentication token.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var authResponse = await _authService.RegisterAsync(request, cancellationToken);
            SetAuthCookie(authResponse.Token, authResponse.ExpiresAt);
            return Ok(authResponse);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during registration.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An error occurred during registration. Please try again."
            });
        }
    }

    /// <summary>
    /// Authenticates a user and returns a token and user profile.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var authResponse = await _authService.LoginAsync(request, cancellationToken);
            SetAuthCookie(authResponse.Token, authResponse.ExpiresAt);
            return Ok(authResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during login.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An error occurred during login. Please try again."
            });
        }
    }

    /// <summary>
    /// Gets the current authenticated user's profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
        {
            return Unauthorized(new { error = "User identity is invalid or expired." });
        }

        try
        {
            var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "User profile not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user profile.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An error occurred while retrieving user profile."
            });
        }
    }

    /// <summary>
    /// Logs out the current user and clears authentication cookies.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/"
        });

        return Ok(new { message = "Logged out successfully." });
    }

    private void SetAuthCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/"
        };

        Response.Cookies.Append(AuthCookieName, token, cookieOptions);
    }
}
