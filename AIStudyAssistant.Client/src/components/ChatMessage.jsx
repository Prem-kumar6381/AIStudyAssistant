import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { FaUser, FaCopy, FaCheck } from 'react-icons/fa';
import { NexoraIcon } from './common/NexoraLogo';
import './ChatMessage.css';

/**
 * Custom CodeBlock component with copy functionality and language tag.
 */
function CodeBlock({ language, codeString }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(codeString);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Fallback
    }
  };

  return (
    <div className="code-block-wrapper">
      <div className="code-block-header">
        <span className="code-block-lang">{language || 'code'}</span>
        <button
          type="button"
          className="code-copy-btn"
          onClick={handleCopy}
          aria-label={copied ? 'Code copied' : 'Copy code'}
        >
          {copied ? <FaCheck className="copy-icon-success" /> : <FaCopy />}
          <span>{copied ? 'Copied' : 'Copy'}</span>
        </button>
      </div>
      <pre className="code-block-pre">
        <code>{codeString}</code>
      </pre>
    </div>
  );
}

/**
 * Renders an individual chat message (user or assistant) or the loading typing indicator.
 * Assistant messages are rendered using ReactMarkdown for rich text formatting.
 */
export default function ChatMessage({ message, isTyping = false }) {
  const [copiedMessage, setCopiedMessage] = useState(false);

  if (isTyping) {
    return (
      <div className="chat-message assistant-message" role="status" aria-live="polite">
        <div className="message-avatar assistant-avatar" aria-hidden="true">
          <NexoraIcon size={18} />
        </div>
        <div className="message-content">
          <div className="message-header">
            <span className="message-author">NEXORA</span>
          </div>
          <div className="message-bubble typing-bubble">
            <div className="typing-indicator" aria-label="NEXORA is thinking">
              <span className="dot"></span>
              <span className="dot"></span>
              <span className="dot"></span>
            </div>
            <span className="typing-text">Thinking...</span>
          </div>
        </div>
      </div>
    );
  }

  const isUser = message?.role === 'user';
  const content = typeof message?.content === 'string' ? message.content : '';

  const handleCopyMessage = async () => {
    try {
      await navigator.clipboard.writeText(content);
      setCopiedMessage(true);
      setTimeout(() => setCopiedMessage(false), 2000);
    } catch {
      // Fallback
    }
  };

  return (
    <div className={`chat-message ${isUser ? 'user-message' : 'assistant-message'}`}>
      <div className={`message-avatar ${isUser ? 'user-avatar' : 'assistant-avatar'}`} aria-hidden="true">
        {isUser ? <FaUser size={14} /> : <NexoraIcon size={18} />}
      </div>
      <div className="message-content">
        <div className="message-header">
          <span className="message-author">{isUser ? 'You' : 'NEXORA'}</span>
          {message?.timestamp && (
            <span className="message-time">
              {new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
            </span>
          )}
          {!isUser && content && (
            <button
              type="button"
              className="message-action-btn"
              onClick={handleCopyMessage}
              title={copiedMessage ? 'Copied' : 'Copy response'}
              aria-label={copiedMessage ? 'Response copied' : 'Copy response'}
            >
              {copiedMessage ? <FaCheck className="copy-icon-success" /> : <FaCopy />}
              <span>{copiedMessage ? 'Copied' : 'Copy'}</span>
            </button>
          )}
        </div>
        <div className="message-bubble">
          {isUser ? (
            <div className="message-text user-text">
              {content.split('\n\n').map((paragraph, idx) => (
                <p key={idx} className="message-paragraph">
                  {paragraph.split('\n').map((line, lineIdx) => (
                    <React.Fragment key={lineIdx}>
                      {line}
                      {lineIdx < paragraph.split('\n').length - 1 && <br />}
                    </React.Fragment>
                  ))}
                </p>
              ))}
            </div>
          ) : (
            <div className="message-text markdown-content">
              <ReactMarkdown
                remarkPlugins={[remarkGfm]}
                components={{
                  a: ({ node: _node, ...props }) => (
                    <a {...props} target="_blank" rel="noopener noreferrer" />
                  ),
                  table: ({ node: _node, ...props }) => (
                    <div className="markdown-table-wrapper">
                      <table {...props} />
                    </div>
                  ),
                  code: ({ node: _node, inline, className, children, ...props }) => {
                    const match = /language-(\w+)/.exec(className || '');
                    const codeString = String(children).replace(/\n$/, '');
                    if (!inline && (match || codeString.includes('\n'))) {
                      return <CodeBlock language={match ? match[1] : ''} codeString={codeString} />;
                    }
                    return (
                      <code className="inline-code" {...props}>
                        {children}
                      </code>
                    );
                  },
                }}
              >
                {content}
              </ReactMarkdown>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
