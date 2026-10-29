using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using AIStudyAssistant.API.DTOs.Auth;
using AIStudyAssistant.API.DTOs.Conversations;

namespace AIStudyAssistant.Tests;

public class ConversationIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConversationIsolationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetConversations_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/conversations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserB_CannotAccessOrModify_UserA_Conversations()
    {
        // 1. Register User A
        var userAReg = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "User Alpha",
            Email = $"usera_{Guid.NewGuid():N}@test.com",
            Password = "PasswordAlpha123!"
        });
        var authA = await userAReg.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authA);

        // 2. Register User B
        var userBReg = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "User Beta",
            Email = $"userb_{Guid.NewGuid():N}@test.com",
            Password = "PasswordBeta123!"
        });
        var authB = await userBReg.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authB);

        // 3. User A creates a conversation
        using var createConvReq = new HttpRequestMessage(HttpMethod.Post, "/api/conversations");
        createConvReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authA.Token);
        createConvReq.Content = JsonContent.Create(new CreateConversationRequest { Title = "Alpha's Private Study Notes" });
        var createResponse = await _client.SendAsync(createConvReq);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var convA = await createResponse.Content.ReadFromJsonAsync<ConversationSummaryDto>();
        Assert.NotNull(convA);

        // 4. User B attempts to read User A's conversation -> MUST return 404 Not Found
        using var readReq = new HttpRequestMessage(HttpMethod.Get, $"/api/conversations/{convA.Id}");
        readReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authB.Token);
        var readResponse = await _client.SendAsync(readReq);
        Assert.Equal(HttpStatusCode.NotFound, readResponse.StatusCode);

        // 5. User B attempts to post message to User A's conversation -> MUST return 404 Not Found
        using var postMsgReq = new HttpRequestMessage(HttpMethod.Post, $"/api/conversations/{convA.Id}/messages");
        postMsgReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authB.Token);
        postMsgReq.Content = JsonContent.Create(new SendMessageRequest { Content = "Intruder message" });
        var postMsgResponse = await _client.SendAsync(postMsgReq);
        Assert.Equal(HttpStatusCode.NotFound, postMsgResponse.StatusCode);

        // 6. User B attempts to rename User A's conversation -> MUST return 404 Not Found
        using var renameReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/conversations/{convA.Id}");
        renameReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authB.Token);
        renameReq.Content = JsonContent.Create(new RenameConversationRequest { Title = "Hacked Title" });
        var renameResponse = await _client.SendAsync(renameReq);
        Assert.Equal(HttpStatusCode.NotFound, renameResponse.StatusCode);

        // 7. User B attempts to delete User A's conversation -> MUST return 404 Not Found
        using var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/conversations/{convA.Id}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authB.Token);
        var deleteResponse = await _client.SendAsync(deleteReq);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        // 8. Verify User A can still read their conversation and it remains unchanged
        using var verifyReq = new HttpRequestMessage(HttpMethod.Get, $"/api/conversations/{convA.Id}");
        verifyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authA.Token);
        var verifyResponse = await _client.SendAsync(verifyReq);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var verifiedConv = await verifyResponse.Content.ReadFromJsonAsync<ConversationDetailDto>();
        Assert.NotNull(verifiedConv);
        Assert.Equal("Alpha's Private Study Notes", verifiedConv.Title);
    }
}
