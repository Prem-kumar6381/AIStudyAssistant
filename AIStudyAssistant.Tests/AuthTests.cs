using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using AIStudyAssistant.API.DTOs.Auth;

namespace AIStudyAssistant.Tests;

public class AuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_Returns200AndToken()
    {
        var email = $"user_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest
        {
            DisplayName = "Test Student",
            Email = email,
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("Test Student", result.User.DisplayName);
        Assert.Equal(email.ToLowerInvariant(), result.User.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        var email = $"duplicate_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest
        {
            DisplayName = "Original User",
            Email = email,
            Password = "Password123!"
        };

        var response1 = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var duplicateRequest = new RegisterRequest
        {
            DisplayName = "Copycat User",
            Email = email,
            Password = "Password456!"
        };

        var response2 = await _client.PostAsJsonAsync("/api/auth/register", duplicateRequest);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndToken()
    {
        var email = $"login_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";

        // Register first
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "Login Tester",
            Email = email,
            Password = password
        });

        // Now login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var result = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(email.ToLowerInvariant(), result.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        var email = $"wrongpw_{Guid.NewGuid():N}@test.com";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "User With Password",
            Email = email,
            Password = "CorrectPassword123!"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword999!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "nonexistent@nowhere.com",
            Password = "SomePassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_Returns200AndProfile()
    {
        var email = $"me_{Guid.NewGuid():N}@test.com";
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "Profile User",
            Email = email,
            Password = "Password123!"
        });

        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var meResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var profile = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(profile);
        Assert.Equal("Profile User", profile.DisplayName);
        Assert.Equal(email.ToLowerInvariant(), profile.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401Unauthorized()
    {
        var meResponse = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }
}
