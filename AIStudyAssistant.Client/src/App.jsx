import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  FaBars,
  FaPlus,
  FaRedo,
  FaExclamationTriangle,
  FaSignInAlt,
  FaUserPlus,
  FaSun,
  FaMoon,
} from 'react-icons/fa';
import Sidebar from './components/Sidebar';
import WelcomeScreen from './components/WelcomeScreen';
import ChatMessage from './components/ChatMessage';
import ChatInput from './components/ChatInput';
import AuthModal from './components/auth/AuthModal';
import ConfirmModal from './components/ConfirmModal';
import { NexoraIcon } from './components/common/NexoraLogo';
import { useAuth } from './context/useAuth';
import { useTheme } from './context/useTheme';
import conversationService from './services/conversationService';
import './App.css';

export default function App() {
  const { user, isAuthenticated, logout } = useAuth();
  const { isDark, toggleTheme } = useTheme();

  const [conversations, setConversations] = useState([]);
  const [activeConversationId, setActiveConversationId] = useState(null);
  const [messages, setMessages] = useState([]);

  const [isLoadingChat, setIsLoadingChat] = useState(false);
  const [isLoadingConversation, setIsLoadingConversation] = useState(false);
  const [isLoadingConversationsList, setIsLoadingConversationsList] = useState(false);

  const [errorMessage, setErrorMessage] = useState(null);
  const [lastFailedPrompt, setLastFailedPrompt] = useState(null);

  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [authModalOpen, setAuthModalOpen] = useState(false);
  const [authModalMode, setAuthModalMode] = useState('login');

  const [confirmModalOpen, setConfirmModalOpen] = useState(false);
  const [conversationToDelete, setConversationToDelete] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const messagesEndRef = useRef(null);

  // Fetch user conversations from backend whenever authenticated user changes
  const fetchConversations = useCallback(async () => {
    if (!isAuthenticated) {
      setConversations([]);
      return;
    }

    setIsLoadingConversationsList(true);
    try {
      const data = await conversationService.getConversations();
      setConversations(data);
    } catch (err) {
      console.error('Failed to load conversations:', err);
    } finally {
      setIsLoadingConversationsList(false);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    let ignore = false;
    if (isAuthenticated) {
      conversationService.getConversations()
        .then((data) => {
          if (!ignore) setConversations(data);
        })
        .catch((err) => console.error('Failed to load conversations:', err));
    } else {
      setConversations([]);
      setActiveConversationId(null);
      setMessages([]);
    }
    return () => {
      ignore = true;
    };
  }, [isAuthenticated]);

  const handleLogout = async () => {
    await logout();
    setActiveConversationId(null);
    setMessages([]);
    setConversations([]);
    setErrorMessage(null);
    setLastFailedPrompt(null);
  };

  // Auto-scroll to bottom of message list on new messages or typing indicator
  useEffect(() => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages, isLoadingChat]);

  // Load a conversation and its messages
  const handleSelectConversation = async (conversationId) => {
    if (isLoadingChat || activeConversationId === conversationId) return;

    setErrorMessage(null);
    setLastFailedPrompt(null);
    setIsLoadingConversation(true);

    try {
      const detail = await conversationService.getConversation(conversationId);
      setActiveConversationId(detail.id);
      setMessages(detail.messages || []);
    } catch (err) {
      console.error('Failed to load conversation details:', err);
      setErrorMessage(err.message || 'Could not load conversation history.');
    } finally {
      setIsLoadingConversation(false);
    }
  };

  // Start a fresh, unpersisted chat session
  const handleNewChat = () => {
    if (isLoadingChat) return;
    setActiveConversationId(null);
    setMessages([]);
    setErrorMessage(null);
    setLastFailedPrompt(null);
  };

  // Handle sending a new prompt to Gemini
  const handleSendMessage = async (userPrompt) => {
    if (!userPrompt || !userPrompt.trim() || isLoadingChat) {
      return;
    }

    // If user is not signed in, prompt them to sign in to save their conversation
    if (!isAuthenticated) {
      setAuthModalMode('login');
      setAuthModalOpen(true);
      return;
    }

    const trimmedPrompt = userPrompt.trim();
    setErrorMessage(null);
    setLastFailedPrompt(null);

    let currentConvId = activeConversationId;

    // If no active conversation exists, create one first
    if (!currentConvId) {
      try {
        const newConv = await conversationService.createConversation();
        currentConvId = newConv.id;
        setActiveConversationId(newConv.id);
        setConversations((prev) => [newConv, ...prev]);
      } catch (err) {
        console.error('Failed to create new conversation:', err);
        setErrorMessage(err.message || 'Could not start new conversation.');
        return;
      }
    }

    // Display the user prompt in the stream immediately
    const optimisticUserMessage = {
      id: `temp-${Date.now()}`,
      conversationId: currentConvId,
      role: 'user',
      content: trimmedPrompt,
      createdAt: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, optimisticUserMessage]);
    setIsLoadingChat(true);

    try {
      const assistantMessage = await conversationService.sendMessage(currentConvId, trimmedPrompt);
      setMessages((prev) => [...prev, assistantMessage]);

      // Refresh conversations list to update title snippets and order
      fetchConversations();
    } catch (err) {
      console.error('Failed to receive Gemini answer:', err);
      const displayError =
        err.message || 'An error occurred while communicating with NEXORA. Please try again.';
      setErrorMessage(displayError);
      setLastFailedPrompt(trimmedPrompt);
    } finally {
      setIsLoadingChat(false);
    }
  };

  // Retry sending the last failed question
  const handleRetry = () => {
    if (lastFailedPrompt && !isLoadingChat) {
      handleSendMessage(lastFailedPrompt);
    }
  };

  // Rename conversation
  const handleRenameConversation = async (conversationId, newTitle) => {
    try {
      const updated = await conversationService.renameConversation(conversationId, newTitle);
      setConversations((prev) =>
        prev.map((c) => (c.id === conversationId ? { ...c, title: updated.title } : c))
      );
    } catch (err) {
      console.error('Failed to rename conversation:', err);
      setErrorMessage(err.message || 'Failed to rename conversation.');
    }
  };

  // Open delete confirmation modal
  const handleDeleteConversation = (conv) => {
    setConversationToDelete(conv);
    setConfirmModalOpen(true);
  };

  // Confirm delete conversation
  const handleConfirmDelete = async () => {
    if (!conversationToDelete) return;

    setIsDeleting(true);
    try {
      await conversationService.deleteConversation(conversationToDelete.id);

      setConversations((prev) => prev.filter((c) => c.id !== conversationToDelete.id));

      // If deleted conversation was active, reset to new chat screen
      if (activeConversationId === conversationToDelete.id) {
        handleNewChat();
      }

      setConfirmModalOpen(false);
      setConversationToDelete(null);
    } catch (err) {
      console.error('Failed to delete conversation:', err);
      setErrorMessage(err.message || 'Failed to delete conversation.');
    } finally {
      setIsDeleting(false);
    }
  };

  // Active topic title for top navbar
  const activeTopic = activeConversationId
    ? conversations.find((c) => c.id === activeConversationId)?.title
    : null;

  return (
    <div className="app-layout">
      {/* Sidebar navigation */}
      <Sidebar
        isOpen={sidebarOpen}
        onClose={() => setSidebarOpen(false)}
        onNewChat={handleNewChat}
        conversations={conversations}
        activeConversationId={activeConversationId}
        onSelectConversation={handleSelectConversation}
        onRenameConversation={handleRenameConversation}
        onDeleteConversation={handleDeleteConversation}
        user={user}
        onOpenAuth={() => {
          setAuthModalMode('login');
          setAuthModalOpen(true);
        }}
        onLogout={handleLogout}
        isLoadingConversations={isLoadingConversationsList}
      />

      {/* Main chat workspace */}
      <main className="main-content">
        {/* Top Navbar */}
        <header className="top-navbar">
          <div className="nav-left">
            <button
              type="button"
              className="menu-toggle-btn"
              onClick={() => setSidebarOpen(true)}
              aria-label="Toggle navigation sidebar"
            >
              <FaBars />
            </button>

            <div className="navbar-title-wrap">
              <NexoraIcon size={24} />
              <span className="navbar-title">NEXORA</span>
            </div>

            {activeTopic && (
              <span className="nav-active-topic" title={activeTopic}>
                &bull; {activeTopic}
              </span>
            )}
          </div>

          <div className="nav-right">
            <span className="gemini-nav-badge" title="Powered by Google Gemini AI">
              <span className="badge-dot"></span>
              Gemini 3.5 Flash
            </span>

            <button
              type="button"
              className="nav-theme-btn"
              onClick={toggleTheme}
              title={isDark ? 'Switch to Light Theme' : 'Switch to Dark Theme'}
              aria-label={isDark ? 'Switch to Light Theme' : 'Switch to Dark Theme'}
            >
              {isDark ? <FaSun className="theme-nav-icon sun" /> : <FaMoon className="theme-nav-icon moon" />}
            </button>

            {isAuthenticated ? (
              <>
                <div className="nav-user-indicator" title={`Signed in as ${user?.email}`}>
                  <div className="nav-user-avatar" aria-hidden="true">
                    {user?.displayName ? user.displayName[0].toUpperCase() : 'U'}
                  </div>
                  <span className="nav-user-name">{user?.displayName}</span>
                </div>

                <button
                  type="button"
                  className="nav-new-chat-btn"
                  onClick={handleNewChat}
                  title="Start a new conversation"
                  disabled={isLoadingChat}
                >
                  <FaPlus className="nav-icon" />
                  <span className="nav-btn-text">New Chat</span>
                </button>
              </>
            ) : (
              <>
                <button
                  type="button"
                  className="nav-auth-btn nav-auth-signin"
                  onClick={() => {
                    setAuthModalMode('login');
                    setAuthModalOpen(true);
                  }}
                >
                  <FaSignInAlt />
                  <span>Sign In</span>
                </button>
                <button
                  type="button"
                  className="nav-auth-btn nav-auth-register"
                  onClick={() => {
                    setAuthModalMode('register');
                    setAuthModalOpen(true);
                  }}
                >
                  <FaUserPlus />
                  <span>Register</span>
                </button>
              </>
            )}
          </div>
        </header>

        {/* Chat message viewport / Welcome screen */}
        <div className="chat-viewport">
          {isLoadingConversation ? (
            <div className="conversation-loading-wrap" role="status">
              <div className="conversation-spinner" aria-hidden="true"></div>
              <span>Loading conversation...</span>
            </div>
          ) : messages.length === 0 ? (
            <WelcomeScreen
              onSelectPrompt={handleSendMessage}
              disabled={isLoadingChat}
            />
          ) : (
            <div className="messages-stream">
              {messages.map((msg) => (
                <ChatMessage
                  key={msg.id}
                  message={{
                    ...msg,
                    timestamp: msg.createdAt || msg.timestamp,
                  }}
                />
              ))}

              {isLoadingChat && <ChatMessage isTyping={true} />}

              {errorMessage && (
                <div className="error-banner" role="alert">
                  <div className="error-icon-text">
                    <FaExclamationTriangle className="error-icon" />
                    <div className="error-content">
                      <span className="error-title">Unable to receive response</span>
                      <p className="error-desc">{errorMessage}</p>
                    </div>
                  </div>
                  {lastFailedPrompt && (
                    <button
                      type="button"
                      className="retry-btn"
                      onClick={handleRetry}
                      disabled={isLoadingChat}
                      title="Retry sending the prompt"
                    >
                      <FaRedo className="retry-icon" />
                      <span>Retry</span>
                    </button>
                  )}
                </div>
              )}

              <div ref={messagesEndRef} />
            </div>
          )}
        </div>

        {/* Bottom chat input */}
        <footer className="chat-footer">
          <ChatInput
            onSendMessage={handleSendMessage}
            isLoading={isLoadingChat}
            disabled={isLoadingConversation}
          />
        </footer>
      </main>

      {/* Authentication Modal */}
      <AuthModal
        isOpen={authModalOpen}
        onClose={() => setAuthModalOpen(false)}
        initialMode={authModalMode}
      />

      {/* Delete Confirmation Modal */}
      <ConfirmModal
        isOpen={confirmModalOpen}
        title="Delete Conversation?"
        message={`Are you sure you want to delete "${conversationToDelete?.title || 'this conversation'}"? Messages in this conversation will be permanently removed.`}
        confirmLabel="Delete Conversation"
        cancelLabel="Cancel"
        isDestructive={true}
        isLoading={isDeleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => {
          if (!isDeleting) {
            setConfirmModalOpen(false);
            setConversationToDelete(null);
          }
        }}
      />
    </div>
  );
}
