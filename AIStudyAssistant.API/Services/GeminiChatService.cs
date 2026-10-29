using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AIStudyAssistant.API.Services;

namespace AIStudyAssistant.API.Services;

public class GeminiChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiChatService> _logger;

    public GeminiChatService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiChatService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetAnswerAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var primaryModel = _configuration["Gemini:Model"] ?? "gemini-3.5-flash";
        var fallbackModel = _configuration["Gemini:FallbackModel"] ?? "gemini-3.5-flash-lite";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = "You are NEXORA, an intelligent, modern, and versatile general-purpose AI assistant. " +
                                   "Your purpose is to help users explore ideas, solve problems, write content, learn concepts, and develop software. " +
                                   "Your primary motto is 'Your next idea starts here.' Provide clear, articulate, helpful, and " +
                                   "well-formatted responses using clean markdown.\n\n" +
                                   "User request: " + message
                        }
                    }
                }
            }
        };

        string activeModel = primaryModel;
        HttpResponseMessage response = await SendGenerateContentRequestAsync(activeModel, apiKey, requestBody, cancellationToken);

        // If the primary model is experiencing a high-demand spike (503) or not found (404),
        // try the verified fallback model to ensure the user request succeeds.
        if (!response.IsSuccessStatusCode &&
            (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.NotFound) &&
            !string.Equals(primaryModel, fallbackModel, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(fallbackModel))
        {
            _logger.LogWarning(
                "Gemini primary model {PrimaryModel} returned HTTP {StatusCode}. Attempting fallback to {FallbackModel}.",
                primaryModel,
                (int)response.StatusCode,
                fallbackModel);

            response.Dispose();
            activeModel = fallbackModel;
            response = await SendGenerateContentRequestAsync(activeModel, apiKey, requestBody, cancellationToken);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var sanitizedDetails = ExtractSanitizedErrorMessage(responseBody, apiKey);

                _logger.LogError(
                    "Gemini API ({Model}) returned HTTP status {StatusCode}. Details: {Details}",
                    activeModel,
                    (int)response.StatusCode,
                    sanitizedDetails);

                throw new HttpRequestException(
                    $"Gemini API request failed with status {(int)response.StatusCode}: {sanitizedDetails}",
                    null,
                    response.StatusCode);
            }

            return ParseGeminiResponse(responseBody);
        }
    }

    private async Task<HttpResponseMessage> SendGenerateContentRequestAsync(
        string modelName,
        string apiKey,
        object requestBody,
        CancellationToken cancellationToken)
    {
        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(modelName)}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(requestBody);

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private string ParseGeminiResponse(string responseBody)
    {
        using var json = JsonDocument.Parse(responseBody);

        if (!json.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            if (json.RootElement.TryGetProperty("promptFeedback", out var promptFeedback) &&
                promptFeedback.TryGetProperty("blockReason", out var blockReason))
            {
                var reason = blockReason.GetString();
                _logger.LogWarning("Gemini prompt was blocked by safety filters: {BlockReason}", reason);
                throw new InvalidOperationException($"The question was flagged by safety filters ({reason}).");
            }

            throw new InvalidOperationException("Gemini did not return any answer candidates.");
        }

        var candidate = candidates[0];

        if (candidate.TryGetProperty("finishReason", out var finishReason))
        {
            var reason = finishReason.GetString();
            if (reason is "SAFETY" or "RECITATION")
            {
                _logger.LogWarning("Gemini response was blocked with finishReason: {FinishReason}", reason);
                throw new InvalidOperationException($"The response was filtered by content policy ({reason}).");
            }
        }

        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts))
        {
            throw new InvalidOperationException("Gemini response missing content parts.");
        }

        var answer = string.Join(
            "\n",
            parts.EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text)));

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException("Gemini returned an empty answer.");
        }

        return answer;
    }

    private static string ExtractSanitizedErrorMessage(string responseBody, string? sensitiveApiKey)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "No response body returned from Gemini.";
        }

        string result;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                var message = errorElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
                var status = errorElement.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

                result = !string.IsNullOrWhiteSpace(status) && !string.IsNullOrWhiteSpace(message)
                    ? $"{status}: {message}"
                    : message ?? status ?? responseBody;
            }
            else
            {
                result = responseBody;
            }
        }
        catch
        {
            result = responseBody;
        }

        if (!string.IsNullOrWhiteSpace(sensitiveApiKey))
        {
            result = result.Replace(sensitiveApiKey, "[REDACTED]");
        }

        return result;
    }
}
