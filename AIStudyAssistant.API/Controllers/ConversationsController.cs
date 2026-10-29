using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIStudyAssistant.API.DTOs.Conversations;
using AIStudyAssistant.API.Services;

namespace AIStudyAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IConversationService conversationService,
        ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all conversations for the authenticated user, ordered by most recently updated.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var conversations = await _conversationService.GetConversationsAsync(userId, cancellationToken);
        return Ok(conversations);
    }

    /// <summary>
    /// Creates a new conversation for the authenticated user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConversationSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest? request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var conversation = await _conversationService.CreateConversationAsync(userId, request?.Title, cancellationToken);
        return CreatedAtAction(nameof(GetConversationById), new { id = conversation.Id }, conversation);
    }

    /// <summary>
    /// Retrieves a conversation and its full message history.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        try
        {
            var conversation = await _conversationService.GetConversationByIdAsync(id, userId, cancellationToken);
            return Ok(conversation);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Conversation not found." });
        }
    }

    /// <summary>
    /// Sends a student message to Gemini within a conversation, saving both the user prompt and the AI answer.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Message content cannot be empty or whitespace." });
        }

        var userId = GetCurrentUserId();

        try
        {
            var assistantMessage = await _conversationService.SendMessageAsync(id, userId, request.Content, cancellationToken);
            return Ok(assistantMessage);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Conversation not found." });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message request was cancelled by the client.");
            return StatusCode(499, new { error = "Request was cancelled." });
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(ex, "Gemini API request failed with status code {StatusCode}: {Message}", ex.StatusCode, ex.Message);

            return ex.StatusCode switch
            {
                HttpStatusCode.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "The AI service is temporarily unavailable or experiencing high demand. Please try again in a few moments."
                }),
                HttpStatusCode.TooManyRequests => StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    error = "AI request limit exceeded. Please wait a moment before asking another question."
                }),
                HttpStatusCode.BadRequest => StatusCode(StatusCodes.Status400BadRequest, new
                {
                    error = "The AI service could not process this question. Please rephrase and try again."
                }),
                HttpStatusCode.NotFound => StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = "The requested AI model could not be found or is unavailable."
                }),
                _ => StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = "An error occurred while communicating with the AI service. Please try again later."
                })
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Chat service validation or content error: {Message}", ex.Message);

            if (ex.Message.Contains("Gemini API key", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "AI service is not configured on the server."
                });
            }

            if (ex.Message.Contains("safety filters", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("content policy", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    error = ex.Message
                });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "The AI service was unable to generate an answer. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during message processing.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An unexpected error occurred while processing your request. Please try again."
            });
        }
    }

    /// <summary>
    /// Renames a conversation title.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ConversationSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RenameConversation(Guid id, [FromBody] RenameConversationRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Title cannot be empty or whitespace." });
        }

        var userId = GetCurrentUserId();

        try
        {
            var updated = await _conversationService.RenameConversationAsync(id, userId, request.Title, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Conversation not found." });
        }
    }

    /// <summary>
    /// Deletes a conversation and all its messages.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        try
        {
            await _conversationService.DeleteConversationAsync(id, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Conversation not found." });
        }
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User identifier is missing or invalid in the token.");
        }
        return userId;
    }
}
