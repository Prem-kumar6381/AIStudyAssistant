namespace AIStudyAssistant.API.Data.Entities;

/// <summary>
/// Represents a persistent study chat conversation belonging to a specific user.
/// </summary>
public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Title { get; set; } = "New Conversation";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
