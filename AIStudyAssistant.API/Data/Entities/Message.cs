namespace AIStudyAssistant.API.Data.Entities;

/// <summary>
/// Represents an individual message (user prompt or assistant answer) in a conversation.
/// </summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }

    /// <summary>
    /// Role of the speaker: "user" or "assistant".
    /// </summary>
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Conversation? Conversation { get; set; }
}
