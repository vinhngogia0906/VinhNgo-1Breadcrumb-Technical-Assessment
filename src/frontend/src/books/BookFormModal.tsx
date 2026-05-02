import { useEffect, useState, type FormEvent } from 'react';
import { Modal } from '../components/Modal';
import type { Book } from '../api/types';

type Props = {
  isOpen: boolean;
  initialBook: Book | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (title: string) => Promise<void> | void;
};

export function BookFormModal({
  isOpen,
  initialBook,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}: Props) {
  const [title, setTitle] = useState('');

  useEffect(() => {
    if (isOpen) setTitle(initialBook?.title ?? '');
  }, [isOpen, initialBook]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const trimmed = title.trim();
    if (trimmed.length === 0) return;
    await onSubmit(trimmed);
  }

  return (
    <Modal
      isOpen={isOpen}
      title={initialBook ? 'Edit book' : 'Add a book'}
      onClose={onClose}
    >
      <form onSubmit={handleSubmit} className="book-form">
        <label>
          Book title
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            maxLength={200}
            autoFocus
            required
          />
        </label>
        {errorMessage && <div className="form-error">{errorMessage}</div>}
        <div className="form-actions">
          <button type="button" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </button>
          <button type="submit" disabled={isSubmitting || title.trim().length === 0}>
            {isSubmitting ? 'Saving...' : initialBook ? 'Save changes' : 'Add book'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
