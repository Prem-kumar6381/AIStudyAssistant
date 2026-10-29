import React from 'react';
import './SuggestionCard.css';

/**
 * Suggestion card displayed on the welcome screen.
 * Clicking immediately populates and submits the question.
 */
export default function SuggestionCard({ title, prompt, icon: Icon, onSelect, disabled }) {
  const handleClick = () => {
    if (!disabled && onSelect) {
      onSelect(prompt);
    }
  };

  const handleKeyDown = (e) => {
    if ((e.key === 'Enter' || e.key === ' ') && !disabled) {
      e.preventDefault();
      handleClick();
    }
  };

  return (
    <div
      className={`suggestion-card ${disabled ? 'disabled' : ''}`}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      role="button"
      tabIndex={disabled ? -1 : 0}
      aria-label={`Ask: ${title}`}
    >
      <div className="suggestion-icon-wrapper">
        {Icon && <Icon className="suggestion-icon" aria-hidden="true" />}
      </div>
      <div className="suggestion-content">
        <h3 className="suggestion-title">{title}</h3>
        <p className="suggestion-prompt">"{prompt}"</p>
      </div>
      <span className="suggestion-arrow" aria-hidden="true">&rarr;</span>
    </div>
  );
}
