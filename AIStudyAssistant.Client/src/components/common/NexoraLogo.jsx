import React from 'react';
import './NexoraLogo.css';

/**
 * NexoraIcon: Clean geometric symbol inspired by the letter N and connected intelligence.
 * Works seamlessly at 16px, 24px, 32px, and 48px+ in both light and dark themes.
 */
export function NexoraIcon({ size = 28, className = '' }) {
  return (
    <svg
      className={`nexora-icon-svg ${className}`}
      width={size}
      height={size}
      viewBox="0 0 32 32"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id="nexoraBgGrad" x1="2" y1="2" x2="30" y2="30" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#635BFF" />
          <stop offset="100%" stopColor="#7E76FF" />
        </linearGradient>
        <linearGradient id="nexoraStrokeGrad" x1="6" y1="6" x2="26" y2="26" gradientUnits="userSpaceOnUse">
          <stop offset="0%" stopColor="#FFFFFF" />
          <stop offset="100%" stopColor="#EBE8FF" />
        </linearGradient>
      </defs>

      {/* Premium squircle container with brand gradient */}
      <rect
        x="2"
        y="2"
        width="28"
        height="28"
        rx="8"
        fill="url(#nexoraBgGrad)"
      />

      {/* Bold geometric 'N' intelligence ribbon */}
      <path
        d="M8.5 24.5V7.5L23.5 24.5V7.5"
        stroke="url(#nexoraStrokeGrad)"
        strokeWidth="3.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />

      {/* Subtle intelligent node accent at the upper apex */}
      <circle
        cx="23.5"
        cy="7.5"
        r="1.8"
        fill="#FFFFFF"
      />
    </svg>
  );
}

/**
 * NexoraLogo: Full horizontal brand logo featuring the geometric N icon and wordmark.
 */
export function NexoraLogo({ size = 26, showTagline = false, className = '' }) {
  return (
    <div className={`nexora-brand-wrapper ${className}`}>
      <div className="nexora-logo-mark">
        <NexoraIcon size={size} />
      </div>
      <div className="nexora-brand-text">
        <div className="nexora-name-row">
          <span className="nexora-wordmark">NEXORA</span>
        </div>
        {showTagline && (
          <span className="nexora-tagline">Your next idea starts here.</span>
        )}
      </div>
    </div>
  );
}

export default NexoraLogo;
