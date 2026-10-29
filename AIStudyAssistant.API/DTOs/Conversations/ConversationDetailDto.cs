namespace AIStudyAssistant.API.DTOs.Conversations;

public class ConversationDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
}
