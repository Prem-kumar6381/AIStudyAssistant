import React from 'react';
import { FaExclamationTriangle, FaTrashAlt, FaTimes } from 'react-icons/fa';
import './ConfirmModal.css';

export default function ConfirmModal({
  isOpen,
  title = 'Confirm Action',
  message = 'Are you sure you want to proceed?',
  confirmLabel = 'Delete',
  cancelLabel = 'Cancel',
  isDestructive = true,
  isLoading = false,
  onConfirm,
  onCancel,
}) {
  if (!isOpen) return null;

  return (
    <div className="confirm-backdrop" onClick={onCancel} role="dialog" aria-modal="true">
      <div className="confirm-modal" onClick={(e) => e.stopPropagation()}>
        <button
          type="button"
          className="confirm-close-btn"
          onClick={onCancel}
          disabled={isLoading}
          aria-label="Close dialog"
        >
          <FaTimes />
        </button>

        <div className="confirm-icon-wrap" aria-hidden="true">
          <FaExclamationTriangle />
        </div>

        <h3 className="confirm-title">{title}</h3>
        <p className="confirm-message">{message}</p>

        <div className="confirm-actions">
          <button
            type="button"
            className="confirm-btn cancel-btn"
            onClick={onCancel}
            disabled={isLoading}
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            className={`confirm-btn ${isDestructive ? 'delete-btn' : 'primary-btn'}`}
            onClick={onConfirm}
            disabled={isLoading}
          >
            {isDestructive && <FaTrashAlt className="btn-icon" />}
            <span>{isLoading ? 'Processing...' : confirmLabel}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
