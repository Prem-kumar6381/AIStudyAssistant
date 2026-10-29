import React, { useState } from 'react';
import {
  FaEnvelope,
  FaLock,
  FaUser,
  FaExclamationCircle,
  FaSpinner,
  FaTimes,
  FaEye,
  FaEyeSlash,
} from 'react-icons/fa';
import { NexoraIcon } from '../common/NexoraLogo';
import { useAuth } from '../../context/useAuth';
import './AuthModal.css';

export default function AuthModal({ isOpen, onClose, initialMode = 'login' }) {
  const { login, register } = useAuth();

  const [mode, setMode] = useState(initialMode); // 'login' | 'register'
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const [clientErrors, setClientErrors] = useState({});
  const [serverError, setServerError] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!isOpen) return null;

  const validate = () => {
    const errors = {};
    const trimmedEmail = email.trim();

    if (!trimmedEmail) {
      errors.email = 'Email address is required.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
      errors.email = 'Please enter a valid email address.';
    }

    if (!password) {
      errors.password = 'Password is required.';
    } else if (password.length < 6) {
      errors.password = 'Password must be at least 6 characters.';
    }

    if (mode === 'register') {
      const trimmedName = displayName.trim();
      if (!trimmedName) {
        errors.displayName = 'Display name is required.';
      } else if (trimmedName.length < 2) {
        errors.displayName = 'Display name must be at least 2 characters.';
      }

      if (password !== confirmPassword) {
        errors.confirmPassword = 'Passwords do not match.';
      }
    }

    setClientErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setServerError(null);

    if (!validate()) return;

    setIsSubmitting(true);
    try {
      if (mode === 'login') {
        await login(email, password);
      } else {
        await register(displayName, email, password);
      }
      onClose();
    } catch (err) {
      setServerError(err.message || 'Authentication failed. Please verify your details and try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const switchMode = (newMode) => {
    setMode(newMode);
    setClientErrors({});
    setServerError(null);
  };

  return (
    <div className="auth-backdrop" onClick={onClose} role="dialog" aria-modal="true">
      <div className="auth-modal" onClick={(e) => e.stopPropagation()}>
        {onClose && (
          <button
            type="button"
            className="auth-close-btn"
            onClick={onClose}
            aria-label="Close authentication dialog"
          >
            <FaTimes />
          </button>
        )}

        <div className="auth-header">
          <div className="auth-icon-wrap" aria-hidden="true">
            <NexoraIcon size={34} />
          </div>
          <h2 className="auth-title">
            {mode === 'login' ? 'Welcome back to NEXORA' : 'Create your NEXORA account'}
          </h2>
          <p className="auth-subtitle">
            {mode === 'login'
              ? 'Sign in to access your conversations, ideas, and history.'
              : 'Your next idea starts here.'}
          </p>
        </div>

        <div className="auth-tabs" role="tablist">
          <button
            type="button"
            className={`auth-tab ${mode === 'login' ? 'active' : ''}`}
            onClick={() => switchMode('login')}
            role="tab"
            aria-selected={mode === 'login'}
          >
            Sign In
          </button>
          <button
            type="button"
            className={`auth-tab ${mode === 'register' ? 'active' : ''}`}
            onClick={() => switchMode('register')}
            role="tab"
            aria-selected={mode === 'register'}
          >
            Create Account
          </button>
        </div>

        {serverError && (
          <div className="auth-error-alert" role="alert">
            <FaExclamationCircle className="alert-icon" />
            <span>{serverError}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form" noValidate>
          {mode === 'register' && (
            <div className="form-group">
              <label htmlFor="displayName">Display Name</label>
              <div className="input-wrap">
                <FaUser className="input-icon" aria-hidden="true" />
                <input
                  id="displayName"
                  type="text"
                  placeholder="e.g. Alex Rivera"
                  value={displayName}
                  onChange={(e) => {
                    setDisplayName(e.target.value);
                    if (clientErrors.displayName) {
                      setClientErrors((prev) => ({ ...prev, displayName: null }));
                    }
                  }}
                  disabled={isSubmitting}
                  autoComplete="name"
                />
              </div>
              {clientErrors.displayName && (
                <span className="field-error">{clientErrors.displayName}</span>
              )}
            </div>
          )}

          <div className="form-group">
            <label htmlFor="authEmail">Email Address</label>
            <div className="input-wrap">
              <FaEnvelope className="input-icon" aria-hidden="true" />
              <input
                id="authEmail"
                type="email"
                placeholder="name@company.com"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value);
                  if (clientErrors.email) {
                    setClientErrors((prev) => ({ ...prev, email: null }));
                  }
                }}
                disabled={isSubmitting}
                autoComplete="email"
              />
            </div>
            {clientErrors.email && (
              <span className="field-error">{clientErrors.email}</span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="authPassword">Password</label>
            <div className="input-wrap">
              <FaLock className="input-icon" aria-hidden="true" />
              <input
                id="authPassword"
                type={showPassword ? 'text' : 'password'}
                placeholder="At least 6 characters"
                value={password}
                onChange={(e) => {
                  setPassword(e.target.value);
                  if (clientErrors.password) {
                    setClientErrors((prev) => ({ ...prev, password: null }));
                  }
                }}
                disabled={isSubmitting}
                autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
              />
              <button
                type="button"
                className="password-toggle-btn"
                onClick={() => setShowPassword((prev) => !prev)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                tabIndex={-1}
              >
                {showPassword ? <FaEyeSlash /> : <FaEye />}
              </button>
            </div>
            {clientErrors.password && (
              <span className="field-error">{clientErrors.password}</span>
            )}
          </div>

          {mode === 'register' && (
            <div className="form-group">
              <label htmlFor="confirmPassword">Confirm Password</label>
              <div className="input-wrap">
                <FaLock className="input-icon" aria-hidden="true" />
                <input
                  id="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  placeholder="Re-enter your password"
                  value={confirmPassword}
                  onChange={(e) => {
                    setConfirmPassword(e.target.value);
                    if (clientErrors.confirmPassword) {
                      setClientErrors((prev) => ({ ...prev, confirmPassword: null }));
                    }
                  }}
                  disabled={isSubmitting}
                  autoComplete="new-password"
                />
                <button
                  type="button"
                  className="password-toggle-btn"
                  onClick={() => setShowConfirmPassword((prev) => !prev)}
                  aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                  tabIndex={-1}
                >
                  {showConfirmPassword ? <FaEyeSlash /> : <FaEye />}
                </button>
              </div>
              {clientErrors.confirmPassword && (
                <span className="field-error">{clientErrors.confirmPassword}</span>
              )}
            </div>
          )}

          <button
            type="submit"
            className="auth-submit-btn"
            disabled={isSubmitting}
          >
            {isSubmitting ? (
              <>
                <FaSpinner className="spinner-icon" />
                <span>{mode === 'login' ? 'Signing In...' : 'Creating Account...'}</span>
              </>
            ) : (
              <span>{mode === 'login' ? 'Sign In' : 'Create Account'}</span>
            )}
          </button>
        </form>

        <div className="auth-footer">
          {mode === 'login' ? (
            <p>
              Don't have an account yet?{' '}
              <button
                type="button"
                className="switch-mode-link"
                onClick={() => switchMode('register')}
              >
                Sign up free
              </button>
            </p>
          ) : (
            <p>
              Already have an account?{' '}
              <button
                type="button"
                className="switch-mode-link"
                onClick={() => switchMode('login')}
              >
                Sign in here
              </button>
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
