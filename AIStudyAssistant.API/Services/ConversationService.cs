using Microsoft.EntityFrameworkCore;
using AIStudyAssistant.API.Data;
using AIStudyAssistant.API.Data.Entities;
using AIStudyAssistant.API.DTOs.Conversations;

namespace AIStudyAssistant.API.Services;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _dbContext;
    private readonly IChatService _chatService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        AppDbContext dbContext,
        IChatService chatService,
        ILogger<ConversationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<ConversationSummaryDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var conversations = await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                MessageCount = c.Messages.Count,
                LastMessageSnippet = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return conversations;
    }

    public async Task<ConversationDetailDto> GetConversationByIdAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId && c.UserId == userId)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            _logger.LogWarning("Conversation {ConversationId} not found or not owned by user {UserId}.", conversationId, userId);
            throw new KeyNotFoundException("Conversation not found.");
        }

        return new ConversationDetailDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(MapToMessageDto)
                .ToList()
        };
    }

    public async Task<ConversationSummaryDto> CreateConversationAsync(Guid userId, string? title, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? "New Conversation" : title.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created conversation {ConversationId} for user {UserId}", conversation.Id, userId);

        return new ConversationSummaryDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            MessageCount = 0,
            LastMessageSnippet = null
        };
    }

    public async Task<ConversationSummaryDto> RenameConversationAsync(Guid conversationId, Guid userId, string newTitle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            throw new ArgumentException("Title cannot be empty or whitespace.", nameof(newTitle));
        }

        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (conversation == null)
        {
            _logger.LogWarning("Cannot rename conversation {ConversationId}: not found for user {UserId}", conversationId, userId);
            throw new KeyNotFoundException("Conversation not found.");
        }

        conversation.Title = newTitle.Trim();
        conversation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var messageCount = await _dbContext.Messages
            .CountAsync(m => m.ConversationId == conversationId, cancellationToken);

        return new ConversationSummaryDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            MessageCount = messageCount,
            LastMessageSnippet = null
        };
    }

    public async Task DeleteConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (conversation == null)
        {
            _logger.LogWarning("Cannot delete conversation {ConversationId}: not found for user {UserId}", conversationId, userId);
            throw new KeyNotFoundException("Conversation not found.");
        }

        _dbContext.Conversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted conversation {ConversationId} for user {UserId}", conversationId, userId);
    }

    public async Task<MessageDto> SendMessageAsync(Guid conversationId, Guid userId, string content, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (conversation == null)
        {
            _logger.LogWarning("Cannot send message: conversation {ConversationId} not found for user {UserId}", conversationId, userId);
            throw new KeyNotFoundException("Conversation not found.");
        }

        var trimmedContent = content.Trim();

        // 1. Create and persist the user's message
        var userMessage = new Message
        {
            ConversationId = conversationId,
            Role = "user",
            Content = trimmedContent,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(userMessage);

        // Auto-update conversation title if it is still default "New Conversation"
        if (conversation.Title == "New Conversation")
        {
            conversation.Title = GenerateTitleFromPrompt(trimmedContent);
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 2. Call the AI service to get the answer
        string answer;
        try
        {
            answer = await _chatService.GetAnswerAsync(trimmedContent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI answer for conversation {ConversationId}", conversationId);
            // Do not save a fabricated assistant answer if Gemini fails
            throw;
        }

        // 3. Persist the AI assistant's completed message
        var assistantMessage = new Message
        {
            ConversationId = conversationId,
            Role = "assistant",
            Content = answer,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(assistantMessage);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToMessageDto(assistantMessage);
    }

    private static string GenerateTitleFromPrompt(string prompt)
    {
        var singleLine = prompt.Replace("\r", " ").Replace("\n", " ").Trim();
        if (singleLine.Length <= 40)
        {
            return singleLine;
        }

        return singleLine[..37].TrimEnd() + "...";
    }

    private static MessageDto MapToMessageDto(Message message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        Role = message.Role,
        Content = message.Content,
        CreatedAt = message.CreatedAt
    };
}
