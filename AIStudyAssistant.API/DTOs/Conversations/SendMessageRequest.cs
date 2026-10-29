using System.ComponentModel.DataAnnotations;

namespace AIStudyAssistant.API.DTOs.Conversations;

public class SendMessageRequest
{
    [Required(ErrorMessage = "Content is required.")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "Message content must be between 1 and 4000 characters.")]
    public string Content { get; set; } = string.Empty;
}
