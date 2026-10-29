namespace AIStudyAssistant.API.Services;

/// <summary>
/// Development mock implementation of <see cref="IChatService"/>.
/// Serves as a placeholder until Azure AI services are configured.
/// </summary>
public class MockChatService : IChatService
{
    private readonly ILogger<MockChatService> _logger;

    public MockChatService(ILogger<MockChatService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetAnswerAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing chat message in MockChatService: {Message}", message);

        // Simulate slight network processing delay so loading state behaves realistically
        await Task.Delay(400, cancellationToken);

        // Clearly labelled development response
        return $"NEXORA (Dev Mode): Your prompt was received successfully.\n\n" +
               $"Prompt: \"{message.Trim()}\"\n\n" +
               "NEXORA is ready to help you explore ideas, solve problems, write content, learn concepts, and build software.";
    }
}
