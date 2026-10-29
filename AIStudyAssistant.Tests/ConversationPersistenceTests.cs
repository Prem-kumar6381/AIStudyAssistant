using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using AIStudyAssistant.API.DTOs.Auth;
using AIStudyAssistant.API.DTOs.Conversations;

namespace AIStudyAssistant.Tests;

public class ConversationPersistenceTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConversationPersistenceTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Conversation_MessagePersistence_And_Lifecycle_Succeeds()
    {
        // 1. Register and get token
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            DisplayName = "Study Scholar",
            Email = $"scholar_{Guid.NewGuid():N}@test.com",
            Password = "ScholarPassword123!"
        });
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        var token = auth.Token;

        // 2. Create a new conversation
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/conversations");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createReq.Content = JsonContent.Create(new CreateConversationRequest());
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var convSummary = await createRes.Content.ReadFromJsonAsync<ConversationSummaryDto>();
        Assert.NotNull(convSummary);
        Assert.Equal("New Conversation", convSummary.Title);

        // 3. Send message 1
        using var msg1Req = new HttpRequestMessage(HttpMethod.Post, $"/api/conversations/{convSummary.Id}/messages");
        msg1Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg1Req.Content = JsonContent.Create(new SendMessageRequest { Content = "What is polymorphism?" });
        var msg1Res = await _client.SendAsync(msg1Req);
        Assert.Equal(HttpStatusCode.OK, msg1Res.StatusCode);
        var answer1 = await msg1Res.Content.ReadFromJsonAsync<MessageDto>();
        Assert.NotNull(answer1);
        Assert.Equal("assistant", answer1.Role);
        Assert.Contains("What is polymorphism?", answer1.Content);

        // 4. Send message 2
        using var msg2Req = new HttpRequestMessage(HttpMethod.Post, $"/api/conversations/{convSummary.Id}/messages");
        msg2Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg2Req.Content = JsonContent.Create(new SendMessageRequest { Content = "Can you give an example in C#?" });
        var msg2Res = await _client.SendAsync(msg2Req);
        Assert.Equal(HttpStatusCode.OK, msg2Res.StatusCode);
        var answer2 = await msg2Res.Content.ReadFromJsonAsync<MessageDto>();
        Assert.NotNull(answer2);
        Assert.Equal("assistant", answer2.Role);

        // 5. Retrieve conversation detail
        using var getDetailReq = new HttpRequestMessage(HttpMethod.Get, $"/api/conversations/{convSummary.Id}");
        getDetailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getDetailRes = await _client.SendAsync(getDetailReq);
        Assert.Equal(HttpStatusCode.OK, getDetailRes.StatusCode);
        var detail = await getDetailRes.Content.ReadFromJsonAsync<ConversationDetailDto>();
        Assert.NotNull(detail);

        // Auto-titling should have renamed from "New Conversation" to prompt snippet
        Assert.NotEqual("New Conversation", detail.Title);
        Assert.Contains("polymorphism", detail.Title, StringComparison.OrdinalIgnoreCase);

        // Verify all 4 messages are present in chronological order: user1, assistant1, user2, assistant2
        Assert.Equal(4, detail.Messages.Count);
        Assert.Equal("user", detail.Messages[0].Role);
        Assert.Equal("What is polymorphism?", detail.Messages[0].Content);
        Assert.Equal("assistant", detail.Messages[1].Role);
        Assert.Equal("user", detail.Messages[2].Role);
        Assert.Equal("Can you give an example in C#?", detail.Messages[2].Content);
        Assert.Equal("assistant", detail.Messages[3].Role);

        // 6. Rename conversation
        using var renameReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/conversations/{convSummary.Id}");
        renameReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        renameReq.Content = JsonContent.Create(new RenameConversationRequest { Title = "Object Oriented Programming (OOP)" });
        var renameRes = await _client.SendAsync(renameReq);
        Assert.Equal(HttpStatusCode.OK, renameRes.StatusCode);
        var renamedSummary = await renameRes.Content.ReadFromJsonAsync<ConversationSummaryDto>();
        Assert.NotNull(renamedSummary);
        Assert.Equal("Object Oriented Programming (OOP)", renamedSummary.Title);

        // 7. Delete conversation
        using var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/conversations/{convSummary.Id}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteRes = await _client.SendAsync(deleteReq);
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // 8. Verify conversation is gone
        using var verifyDeletedReq = new HttpRequestMessage(HttpMethod.Get, $"/api/conversations/{convSummary.Id}");
        verifyDeletedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var verifyDeletedRes = await _client.SendAsync(verifyDeletedReq);
        Assert.Equal(HttpStatusCode.NotFound, verifyDeletedRes.StatusCode);
    }
}
