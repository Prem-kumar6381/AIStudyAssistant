using System.ComponentModel.DataAnnotations;

namespace AIStudyAssistant.API.DTOs.Conversations;

public class CreateConversationRequest
{
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string? Title { get; set; }
}
