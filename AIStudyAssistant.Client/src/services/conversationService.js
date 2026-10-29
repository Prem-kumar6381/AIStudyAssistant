import apiClient from './apiClient';

/**
 * Lists all conversations for the authenticated user.
 * @returns {Promise<Array<{ id: string, title: string, createdAt: string, updatedAt: string, messageCount: number }>>}
 */
export async function getConversations() {
  const response = await apiClient.get('/api/conversations');
  return response.data || [];
}

/**
 * Retrieves a single conversation with its complete message history.
 * @param {string} id - The conversation UUID.
 * @returns {Promise<{ id: string, title: string, messages: Array<object> }>}
 */
export async function getConversation(id) {
  const response = await apiClient.get(`/api/conversations/${id}`);
  return response.data;
}

/**
 * Creates a new conversation.
 * @param {string} [title] - Optional title for the conversation.
 * @returns {Promise<object>}
 */
export async function createConversation(title) {
  const response = await apiClient.post('/api/conversations', {
    title: title ? title.trim() : null,
  });
  return response.data;
}

/**
 * Sends a message within a conversation to Gemini and saves both user and assistant messages.
 * @param {string} conversationId - The conversation UUID.
 * @param {string} content - The student prompt.
 * @returns {Promise<{ id: string, conversationId: string, role: string, content: string, createdAt: string }>}
 */
export async function sendMessage(conversationId, content) {
  if (!content || !content.trim()) {
    throw new Error('Message content cannot be empty.');
  }

  const response = await apiClient.post(`/api/conversations/${conversationId}/messages`, {
    content: content.trim(),
  });
  return response.data;
}

/**
 * Renames a conversation.
 * @param {string} conversationId - The conversation UUID.
 * @param {string} title - The new title.
 * @returns {Promise<object>}
 */
export async function renameConversation(conversationId, title) {
  if (!title || !title.trim()) {
    throw new Error('Title cannot be empty.');
  }

  const response = await apiClient.patch(`/api/conversations/${conversationId}`, {
    title: title.trim(),
  });
  return response.data;
}

/**
 * Deletes a conversation and all its messages.
 * @param {string} conversationId - The conversation UUID.
 * @returns {Promise<void>}
 */
export async function deleteConversation(conversationId) {
  await apiClient.delete(`/api/conversations/${conversationId}`);
}

/**
 * Backwards compatibility helper for standalone /api/chat endpoint.
 */
export async function sendChatMessage(message) {
  const response = await apiClient.post('/api/chat', {
    message: message.trim(),
  });
  return response.data;
}

export default {
  getConversations,
  getConversation,
  createConversation,
  sendMessage,
  renameConversation,
  deleteConversation,
  sendChatMessage,
};
