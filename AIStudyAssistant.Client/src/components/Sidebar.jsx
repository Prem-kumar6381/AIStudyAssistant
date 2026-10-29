import React, { useState, useMemo, useEffect, useCallback } from 'react';
import {
  FaPlus,
  FaCommentAlt,
  FaTrashAlt,
  FaTimes,
  FaPencilAlt,
  FaCheck,
  FaSignOutAlt,
  FaSignInAlt,
  FaUserCircle,
  FaSearch,
  FaSun,
  FaMoon,
} from 'react-icons/fa';
import { NexoraLogo } from './common/NexoraLogo';
import { useTheme } from '../context/useTheme';
import './Sidebar.css';

/**
 * Responsive Sidebar & Navigation Drawer for NEXORA.
 * Behaves as a fixed collapsible sidebar on desktop (>=1024px)
 * and an accessible modal slide-out drawer on tablet and mobile (<1024px).
 */
export default function Sidebar({
  isOpen,
  onClose,
  onNewChat,
  conversations = [],
  activeConversationId,
  onSelectConversation,
  onRenameConversation,
  onDeleteConversation,
  user,
  onOpenAuth,
  onLogout,
  isLoadingConversations = false,
}) {
  const { isDark, toggleTheme } = useTheme();
  const [searchQuery, setSearchQuery] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [editTitle, setEditTitle] = useState('');

  // Close drawer if currently in tablet/mobile drawer mode
  const closeDrawerIfMobile = useCallback(() => {
    if (window.innerWidth < 1024 && onClose) {
      onClose();
    }
  }, [onClose]);

  // Handle Escape key to close the drawer
  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  // Prevent background page scrolling while the mobile drawer is open
  useEffect(() => {
    if (isOpen && window.innerWidth < 1024) {
      const originalOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
      return () => {
        document.body.style.overflow = originalOverflow;
      };
    }
  }, [isOpen]);

  const filteredConversations = useMemo(() => {
    if (!searchQuery.trim()) return conversations;
    const query = searchQuery.toLowerCase().trim();
    return conversations.filter((c) =>
      c.title && c.title.toLowerCase().includes(query)
    );
  }, [conversations, searchQuery]);

  const startEditing = (conv, e) => {
    e.stopPropagation();
    setEditingId(conv.id);
    setEditTitle(conv.title);
  };

  const handleSaveRename = async (convId, e) => {
    if (e) e.stopPropagation();
    if (!editTitle || !editTitle.trim()) {
      setEditingId(null);
      return;
    }

    try {
      await onRenameConversation(convId, editTitle.trim());
    } finally {
      setEditingId(null);
    }
  };

  const handleRenameKeyDown = (convId, e) => {
    if (e.key === 'Enter') {
      handleSaveRename(convId, e);
    } else if (e.key === 'Escape') {
      setEditingId(null);
    }
  };

  const handleDeleteClick = (conv, e) => {
    e.stopPropagation();
    onDeleteConversation(conv);
  };

  return (
    <>
      {/* Accessible mobile/tablet backdrop */}
      {isOpen && (
        <div
          className="sidebar-backdrop"
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      <aside
        className={`sidebar ${isOpen ? 'sidebar-open' : ''}`}
        aria-label="Navigation and chat history"
        aria-hidden={!isOpen && typeof window !== 'undefined' && window.innerWidth < 1024}
      >
        {/* Top Branding & Controls */}
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <NexoraLogo size={28} />
          </div>

          <div className="sidebar-header-actions">
            <button
              type="button"
              className="theme-toggle-btn"
              onClick={toggleTheme}
              title={isDark ? 'Switch to Light Theme' : 'Switch to Dark Theme'}
              aria-label={isDark ? 'Switch to Light Theme' : 'Switch to Dark Theme'}
            >
              {isDark ? <FaSun className="theme-icon sun" /> : <FaMoon className="theme-icon moon" />}
            </button>

            <button
              type="button"
              className="sidebar-close-btn"
              onClick={onClose}
              title="Close navigation drawer"
              aria-label="Close navigation drawer"
            >
              <FaTimes />
            </button>
          </div>
        </div>

        {/* Action: New Chat */}
        <div className="sidebar-action-section">
          <button
            type="button"
            className="new-chat-btn"
            onClick={() => {
              onNewChat();
              closeDrawerIfMobile();
            }}
            title="Start a new conversation"
          >
            <FaPlus className="btn-icon" />
            <span>New Chat</span>
          </button>
        </div>

        {/* Search Conversations */}
        {user && conversations.length > 2 && (
          <div className="sidebar-search-section">
            <div className="sidebar-search-wrapper">
              <FaSearch className="search-icon" aria-hidden="true" />
              <input
                type="text"
                className="sidebar-search-input"
                placeholder="Search chats..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                aria-label="Search conversation history"
              />
              {searchQuery && (
                <button
                  type="button"
                  className="search-clear-btn"
                  onClick={() => setSearchQuery('')}
                  aria-label="Clear search"
                >
                  <FaTimes />
                </button>
              )}
            </div>
          </div>
        )}

        {/* Chat History Section */}
        <div className="sidebar-history-section">
          <div className="section-label">
            <span>Conversations</span>
          </div>

          <div className="history-list">
            {!user ? (
              <div className="history-auth-prompt">
                <p>Sign in to save and access your conversations from any device.</p>
                <button
                  type="button"
                  className="sidebar-auth-btn"
                  onClick={() => {
                    onOpenAuth();
                    closeDrawerIfMobile();
                  }}
                >
                  <FaSignInAlt />
                  <span>Sign In / Register</span>
                </button>
              </div>
            ) : isLoadingConversations ? (
              <div className="history-loading">
                <span className="history-spinner"></span>
                <span>Loading conversations...</span>
              </div>
            ) : filteredConversations.length > 0 ? (
              filteredConversations.map((conv) => {
                const isActive = conv.id === activeConversationId;
                const isEditing = editingId === conv.id;

                return (
                  <div
                    key={conv.id}
                    className={`history-item-wrap ${isActive ? 'active' : ''}`}
                    onClick={() => {
                      if (!isEditing) {
                        onSelectConversation(conv.id);
                        closeDrawerIfMobile();
                      }
                    }}
                    role="button"
                    tabIndex={0}
                    onKeyDown={(e) => {
                      if ((e.key === 'Enter' || e.key === ' ') && !isEditing) {
                        e.preventDefault();
                        onSelectConversation(conv.id);
                        closeDrawerIfMobile();
                      }
                    }}
                    aria-label={`Open conversation: ${conv.title}`}
                  >
                    <FaCommentAlt className="history-icon" aria-hidden="true" />

                    {isEditing ? (
                      <div className="history-edit-row" onClick={(e) => e.stopPropagation()}>
                        <input
                          type="text"
                          className="history-rename-input"
                          value={editTitle}
                          onChange={(e) => setEditTitle(e.target.value)}
                          onKeyDown={(e) => handleRenameKeyDown(conv.id, e)}
                          autoFocus
                          maxLength={200}
                          aria-label="Edit conversation title"
                        />
                        <button
                          type="button"
                          className="history-inline-btn save-btn"
                          onClick={(e) => handleSaveRename(conv.id, e)}
                          title="Save Title"
                          aria-label="Save title"
                        >
                          <FaCheck />
                        </button>
                        <button
                          type="button"
                          className="history-inline-btn cancel-btn"
                          onClick={(e) => {
                            e.stopPropagation();
                            setEditingId(null);
                          }}
                          title="Cancel"
                          aria-label="Cancel"
                        >
                          <FaTimes />
                        </button>
                      </div>
                    ) : (
                      <>
                        <span className="history-title" title={conv.title}>
                          {conv.title}
                        </span>

                        <div className="history-actions" onClick={(e) => e.stopPropagation()}>
                          <button
                            type="button"
                            className="history-action-btn"
                            onClick={(e) => startEditing(conv, e)}
                            title="Rename conversation"
                            aria-label={`Rename ${conv.title}`}
                          >
                            <FaPencilAlt />
                          </button>
                          <button
                            type="button"
                            className="history-action-btn delete-action"
                            onClick={(e) => handleDeleteClick(conv, e)}
                            title="Delete conversation"
                            aria-label={`Delete ${conv.title}`}
                          >
                            <FaTrashAlt />
                          </button>
                        </div>
                      </>
                    )}
                  </div>
                );
              })
            ) : searchQuery ? (
              <div className="history-placeholder">
                <p>No matching conversations found.</p>
                <small>Try a different keyword.</small>
              </div>
            ) : (
              <div className="history-placeholder">
                <p>No conversations yet.</p>
                <small>Start typing or select a suggestion to begin.</small>
              </div>
            )}
          </div>
        </div>

        {/* User Profile / Authentication Footer */}
        <div className="sidebar-footer">
          {user ? (
            <div className="user-profile-card">
              <div className="user-info">
                <div className="user-avatar-badge" aria-hidden="true">
                  {user.displayName ? user.displayName[0].toUpperCase() : <FaUserCircle />}
                </div>
                <div className="user-details">
                  <span className="user-name">{user.displayName}</span>
                  <span className="user-email" title={user.email}>{user.email}</span>
                </div>
              </div>

              <button
                type="button"
                className="logout-btn"
                onClick={() => {
                  onLogout();
                  closeDrawerIfMobile();
                }}
                title="Sign out of NEXORA"
                aria-label="Sign out"
              >
                <FaSignOutAlt />
              </button>
            </div>
          ) : (
            <button
              type="button"
              className="sidebar-signin-trigger"
              onClick={() => {
                onOpenAuth();
                closeDrawerIfMobile();
              }}
            >
              <FaSignInAlt className="signin-icon" />
              <span>Sign In / Register</span>
            </button>
          )}
        </div>
      </aside>
    </>
  );
}
