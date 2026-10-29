using System.ComponentModel.DataAnnotations;

namespace AIStudyAssistant.API.DTOs;

/// <summary>
/// Data transfer object representing the user chat request.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The message prompt or question submitted by the user.
    /// </summary>
    [Required(ErrorMessage = "Message is required.")]
    public string Message { get; set; } = string.Empty;
}
