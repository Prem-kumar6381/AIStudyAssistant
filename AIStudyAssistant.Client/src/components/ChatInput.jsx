import React, { useState, useRef, useEffect } from 'react';
import { FaPaperPlane, FaTimesCircle } from 'react-icons/fa';
import './ChatInput.css';

/**
 * Chat input component supporting multiline input, Enter to send, Shift+Enter for newline,
 * empty validation, and loading states for NEXORA.
 */
export default function ChatInput({ onSendMessage, isLoading, disabled }) {
  const [input, setInput] = useState('');
  const [validationError, setValidationError] = useState('');
  const textareaRef = useRef(null);

  // Auto-resize textarea height to accommodate multiline text
  useEffect(() => {
    const textarea = textareaRef.current;
    if (textarea) {
      textarea.style.height = 'auto';
      const scrollHeight = textarea.scrollHeight;
      textarea.style.height = `${Math.min(scrollHeight, 180)}px`;
    }
  }, [input]);

  const handleSubmit = (e) => {
    if (e) e.preventDefault();

    if (isLoading || disabled) return;

    const trimmed = input.trim();
    if (!trimmed) {
      setValidationError('Please type a message before sending.');
      if (textareaRef.current) {
        textareaRef.current.focus();
      }
      return;
    }

    setValidationError('');
    onSendMessage(trimmed);
    setInput('');

    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleChange = (e) => {
    setInput(e.target.value);
    if (validationError && e.target.value.trim()) {
      setValidationError('');
    }
  };

  const handleClear = () => {
    setInput('');
    setValidationError('');
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.focus();
    }
  };

  const isSendDisabled = isLoading || disabled || !input.trim();

  return (
    <div className="chat-input-container">
      <form className="chat-input-form" onSubmit={handleSubmit} noValidate>
        <div className={`chat-input-wrapper ${validationError ? 'has-error' : ''}`}>
          <textarea
            ref={textareaRef}
            className="chat-textarea"
            placeholder="Ask questions, explore ideas, write, learn, and build with AI..."
            value={input}
            onChange={handleChange}
            onKeyDown={handleKeyDown}
            disabled={isLoading || disabled}
            rows={1}
            aria-label="Message NEXORA"
          />

          {input.length > 0 && !isLoading && (
            <button
              type="button"
              className="chat-clear-input-btn"
              onClick={handleClear}
              title="Clear input"
              aria-label="Clear input text"
            >
              <FaTimesCircle />
            </button>
          )}

          <button
            type="submit"
            className="chat-send-btn"
            disabled={isSendDisabled}
            title={isLoading ? 'NEXORA is thinking...' : 'Send message (Enter)'}
            aria-label="Send message"
          >
            <FaPaperPlane className="send-icon" />
          </button>
        </div>

        {validationError && (
          <div className="chat-validation-error" role="alert">
            {validationError}
          </div>
        )}

        <div className="chat-input-hint">
          <span>NEXORA can make mistakes. Verify important information.</span>
        </div>
      </form>
    </div>
  );
}
