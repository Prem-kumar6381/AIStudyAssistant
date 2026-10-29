using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Moq;
using Xunit;
using AIStudyAssistant.API.DTOs.Auth;
using AIStudyAssistant.API.DTOs.Conversations;

namespace AIStudyAssistant.Tests;

public class GeminiErrorHandlingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GeminiErrorHandlingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendMessage_WhenGeminiReturns503_Returns503AndNoFabricatedResponse()
    {
        // 1. Setup mock to simulate 503 Service Unavailable from Gemini
        _factory.MockChatService
            .Setup(s => s.GetAnswerAsync(It.Is<string>(m => m.Contains("503-test")), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable));

        // 2. Register user & create conversation
        var regRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "503 Tester",
            Email = $"gemini503_{Guid.NewGuid():N}@test.com",
            Password = "Password123!"
        });
        var auth = await regRes.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/conversations");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var createRes = await _client.SendAsync(createReq);
        var conv = await createRes.Content.ReadFromJsonAsync<ConversationSummaryDto>();
        Assert.NotNull(conv);

        // 3. Send message triggering 503
        using var sendReq = new HttpRequestMessage(HttpMethod.Post, $"/api/conversations/{conv.Id}/messages");
        sendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        sendReq.Content = JsonContent.Create(new SendMessageRequest { Content = "503-test question" });
        var sendRes = await _client.SendAsync(sendReq);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, sendRes.StatusCode);

        // 4. Verify conversation detail has NO assistant message (no fabricated response)
        using var getDetailReq = new HttpRequestMessage(HttpMethod.Get, $"/api/conversations/{conv.Id}");
        getDetailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var getDetailRes = await _client.SendAsync(getDetailReq);
        var detail = await getDetailRes.Content.ReadFromJsonAsync<ConversationDetailDto>();
        Assert.NotNull(detail);

        // Only the user's message was saved, no fabricated assistant message!
        Assert.DoesNotContain(detail.Messages, m => m.Role == "assistant");
    }

    [Fact]
    public async Task SendMessage_WhenGeminiQuotaExceeded_Returns429TooManyRequests()
    {
        _factory.MockChatService
            .Setup(s => s.GetAnswerAsync(It.Is<string>(m => m.Contains("quota-test")), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Quota exceeded", null, HttpStatusCode.TooManyRequests));

        var regRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "Quota Tester",
            Email = $"quota_{Guid.NewGuid():N}@test.com",
            Password = "Password123!"
        });
        var auth = await regRes.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/conversations");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var createRes = await _client.SendAsync(createReq);
        var conv = await createRes.Content.ReadFromJsonAsync<ConversationSummaryDto>();
        Assert.NotNull(conv);

        using var sendReq = new HttpRequestMessage(HttpMethod.Post, $"/api/conversations/{conv.Id}/messages");
        sendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        sendReq.Content = JsonContent.Create(new SendMessageRequest { Content = "quota-test question" });
        var sendRes = await _client.SendAsync(sendReq);

        Assert.Equal(HttpStatusCode.TooManyRequests, sendRes.StatusCode);
    }

    [Fact]
    public async Task SendMessage_WhenSafetyFilterBlocks_Returns400BadRequest()
    {
        _factory.MockChatService
            .Setup(s => s.GetAnswerAsync(It.Is<string>(m => m.Contains("safety-test")), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The question was flagged by safety filters (HARM)."));

        var regRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "Safety Tester",
            Email = $"safety_{Guid.NewGuid():N}@test.com",
            Password = "Password123!"
        });
        var auth = await regRes.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/conversations");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var createRes = await _client.SendAsync(createReq);
        var conv = await createRes.Content.ReadFromJsonAsync<ConversationSummaryDto>();
        Assert.NotNull(conv);

        using var sendReq = new HttpRequestMessage(HttpMethod.Post, $"/api/conversations/{conv.Id}/messages");
        sendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        sendReq.Content = JsonContent.Create(new SendMessageRequest { Content = "safety-test question" });
        var sendRes = await _client.SendAsync(sendReq);

        Assert.Equal(HttpStatusCode.BadRequest, sendRes.StatusCode);
    }

    [Fact]
    public async Task SendMessage_EmptyOrWhitespaceMessage_Returns400BadRequest()
    {
        var regRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "Validation Tester",
            Email = $"val_{Guid.NewGuid():N}@test.com",
            Password = "Password123!"
        });
        var auth = await regRes.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/conversations");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var createRes = await _client.SendAsync(createReq);
        var conv = await createRes.Content.ReadFromJsonAsync<ConversationSummaryDto>();
        Assert.NotNull(conv);

        using var sendReq = new HttpRequestMessage(HttpMethod.Post, $"/api/conversations/{conv.Id}/messages");
        sendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        sendReq.Content = JsonContent.Create(new SendMessageRequest { Content = "   " });
        var sendRes = await _client.SendAsync(sendReq);

        Assert.Equal(HttpStatusCode.BadRequest, sendRes.StatusCode);
    }
}
