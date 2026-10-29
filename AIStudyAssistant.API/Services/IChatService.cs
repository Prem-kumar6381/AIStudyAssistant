namespace AIStudyAssistant.API.Services;

/// <summary>
/// Defines the contract for processing study assistant chat interactions.
/// Allows swapping between mock services and live cloud AI providers (e.g. Azure AI) seamlessly.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Asynchronously processes a user question and returns the assistant's answer.
    /// </summary>
    /// <param name="message">The student's question or prompt.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The answer generated for the user question.</returns>
    Task<string> GetAnswerAsync(string message, CancellationToken cancellationToken = default);
}
