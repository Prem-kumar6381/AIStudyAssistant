namespace AIStudyAssistant.API.DTOs;

/// <summary>
/// Data transfer object representing the assistant chat response.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The answer or response text generated for the user prompt.
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    public ChatResponse()
    {
    }

    public ChatResponse(string answer)
    {
        Answer = answer;
    }
}
