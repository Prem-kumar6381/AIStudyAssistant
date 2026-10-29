using Microsoft.AspNetCore.Mvc;
using AIStudyAssistant.API.DTOs;
using AIStudyAssistant.API.Services;

namespace AIStudyAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes a student question and returns an AI/study assistant answer.
    /// </summary>
    /// <param name="request">The chat request containing the user's message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ChatResponse"/> containing the answer, or a 400 Bad Request if the message is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostMessage([FromBody] ChatRequest? request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            _logger.LogWarning("Invalid chat request received: message is null, empty, or whitespace.");
            return BadRequest(new { error = "Message cannot be empty or whitespace." });
        }

        try
        {
            string answer = await _chatService.GetAnswerAsync(request.Message, cancellationToken);
            return Ok(new ChatResponse(answer));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat request was cancelled by the client.");
            return StatusCode(499, new { error = "Request was cancelled." });
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(ex, "Gemini API request failed with status code {StatusCode}: {Message}", ex.StatusCode, ex.Message);

            return ex.StatusCode switch
            {
                System.Net.HttpStatusCode.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "The AI service is temporarily unavailable or experiencing high demand. Please try again in a few moments."
                }),
                System.Net.HttpStatusCode.TooManyRequests => StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    error = "AI request limit exceeded. Please wait a moment before asking another question."
                }),
                System.Net.HttpStatusCode.BadRequest => StatusCode(StatusCodes.Status400BadRequest, new
                {
                    error = "The AI service could not process this question. Please rephrase and try again."
                }),
                System.Net.HttpStatusCode.NotFound => StatusCode(StatusCodes.Status502BadGateway, new
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
            _logger.LogError(ex, "Unexpected error occurred during chat processing.");
            // Do not expose stack traces or internal details in responses
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An unexpected error occurred while processing your request. Please try again."
            });
        }
    }
}
