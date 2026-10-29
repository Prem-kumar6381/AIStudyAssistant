import React from 'react';
import { FaLightbulb, FaPenNib, FaCode, FaCompass } from 'react-icons/fa';
import { NexoraIcon } from './common/NexoraLogo';
import SuggestionCard from './SuggestionCard';
import './WelcomeScreen.css';

const SUGGESTIONS = [
  {
    id: 'explain',
    title: 'Explain a complex concept',
    prompt: 'Explain quantum computing in simple terms with an everyday analogy.',
    icon: FaLightbulb,
  },
  {
    id: 'write',
    title: 'Help me write something',
    prompt: 'Draft an executive summary for a SaaS product launch proposal.',
    icon: FaPenNib,
  },
  {
    id: 'debug',
    title: 'Debug a programming problem',
    prompt: 'How do I resolve memory leaks in asynchronous React useEffect hooks?',
    icon: FaCode,
  },
  {
    id: 'brainstorm',
    title: 'Brainstorm new ideas',
    prompt: 'Give me 5 creative marketing strategies for an AI productivity app.',
    icon: FaCompass,
  },
];

/**
 * Welcome screen for NEXORA.
 * Displays bold brand identity, tagline, supporting description, and suggestions.
 */
export default function WelcomeScreen({ onSelectPrompt, disabled }) {
  return (
    <div className="welcome-screen">
      <div className="welcome-hero-mark">
        <div className="welcome-icon-glow">
          <NexoraIcon size={46} />
        </div>
      </div>

      <h1 className="welcome-title">NEXORA</h1>
      <h2 className="welcome-tagline">Your next idea starts here.</h2>
      <p className="welcome-subtitle">Ask questions, explore ideas, write, learn, and build with AI.</p>

      <div className="welcome-suggestions-grid">
        {SUGGESTIONS.map((item) => (
          <SuggestionCard
            key={item.id}
            title={item.title}
            prompt={item.prompt}
            icon={item.icon}
            onSelect={onSelectPrompt}
            disabled={disabled}
          />
        ))}
      </div>

      <div className="welcome-meta-pill">
        <span className="meta-dot" />
        <span>Powered by Gemini 3.5 &bull; Next-Gen Intelligence</span>
      </div>
    </div>
  );
}
