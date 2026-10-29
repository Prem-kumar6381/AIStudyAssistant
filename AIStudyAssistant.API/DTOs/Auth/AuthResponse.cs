namespace AIStudyAssistant.API.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    public AuthResponse()
    {
    }

    public AuthResponse(string token, UserDto user, DateTime expiresAt)
    {
        Token = token;
        User = user;
        ExpiresAt = expiresAt;
    }
}
