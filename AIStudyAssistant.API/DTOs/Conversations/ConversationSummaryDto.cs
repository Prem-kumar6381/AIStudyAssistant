namespace AIStudyAssistant.API.DTOs.Conversations;

public class ConversationSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public string? LastMessageSnippet { get; set; }
}
