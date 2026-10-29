using AIStudyAssistant.API.DTOs.Conversations;

namespace AIStudyAssistant.API.Services;

public interface IConversationService
{
    Task<List<ConversationSummaryDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ConversationDetailDto> GetConversationByIdAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<ConversationSummaryDto> CreateConversationAsync(Guid userId, string? title, CancellationToken cancellationToken = default);
    Task<ConversationSummaryDto> RenameConversationAsync(Guid conversationId, Guid userId, string newTitle, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<MessageDto> SendMessageAsync(Guid conversationId, Guid userId, string content, CancellationToken cancellationToken = default);
}
