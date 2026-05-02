import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { booksApi } from '../api/books';
import { readErrorMessage } from '../api/client';
import type { AvailabilityFilter, Book, PagedResult } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { Pagination } from '../components/Pagination';
import { useDebouncedValue } from '../hooks/useDebouncedValue';
import { BookFormModal } from './BookFormModal';
import { BookTable } from './BookTable';

const PAGE_SIZE = 5;

const emptyPage: PagedResult<Book> = {
  items: [],
  page: 1,
  pageSize: PAGE_SIZE,
  totalCount: 0,
  totalPages: 0,
};

export function LibraryPage() {
  const { user, logout } = useAuth();
  const [searchInput, setSearchInput] = useState('');
  const [availability, setAvailability] = useState<AvailabilityFilter>('All');
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<Book>>(emptyPage);
  const [isLoading, setIsLoading] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingBook, setEditingBook] = useState<Book | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [modalError, setModalError] = useState<string | null>(null);

  const [isMutating, setIsMutating] = useState(false);

  const debouncedSearch = useDebouncedValue(searchInput.trim(), 250);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setPageError(null);
    try {
      const result = await booksApi.search({
        search: debouncedSearch || undefined,
        availability,
        page,
        pageSize: PAGE_SIZE,
      });
      setData(result);
      // If the server returned 0 items but we're past page 1, walk back.
      if (result.items.length === 0 && result.page > 1 && result.totalPages > 0) {
        setPage(result.totalPages);
      }
    } catch (err) {
      setPageError(readErrorMessage(err, 'Failed to load books'));
    } finally {
      setIsLoading(false);
    }
  }, [debouncedSearch, availability, page]);

  useEffect(() => {
    reload();
  }, [reload]);

  // Reset to page 1 when filters change.
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, availability]);

  const openAdd = () => {
    setEditingBook(null);
    setModalError(null);
    setIsModalOpen(true);
  };

  const openEdit = (book: Book) => {
    setEditingBook(book);
    setModalError(null);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    if (isSubmitting) return;
    setIsModalOpen(false);
    setEditingBook(null);
    setModalError(null);
  };

  const handleSubmit = async (title: string) => {
    setIsSubmitting(true);
    setModalError(null);
    try {
      if (editingBook) {
        await booksApi.update(editingBook.id, title);
      } else {
        await booksApi.create(title);
      }
      setIsModalOpen(false);
      setEditingBook(null);
      await reload();
    } catch (err) {
      setModalError(readErrorMessage(err, 'Failed to save book'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const runMutation = async (action: () => Promise<unknown>) => {
    setIsMutating(true);
    setPageError(null);
    try {
      await action();
      await reload();
    } catch (err) {
      setPageError(readErrorMessage(err, 'Action failed'));
    } finally {
      setIsMutating(false);
    }
  };

  const handleDelete = (book: Book) => {
    if (!window.confirm(`Delete "${book.title}"?`)) return;
    void runMutation(() => booksApi.remove(book.id));
  };

  const handleBorrow = (book: Book) => void runMutation(() => booksApi.borrow(book.id));
  const handleReturn = (book: Book) => void runMutation(() => booksApi.return(book.id));

  const currentUserId = user?.id ?? '';

  const summary = useMemo(() => {
    if (data.totalCount === 0) return 'No books';
    const start = (data.page - 1) * data.pageSize + 1;
    const end = Math.min(data.page * data.pageSize, data.totalCount);
    return `${start}–${end} of ${data.totalCount}`;
  }, [data]);

  return (
    <div className="library-page">
      <header className="page-header">
        <div>
          <h1>Library</h1>
          <p className="muted">Crumb-to-Crumb book lending</p>
        </div>
        <div className="user-info">
          {user?.role === 'Admin' && (
            <Link to="/admin/activity" className="header-link">Activity log</Link>
          )}
          <span>{user?.displayName}{user?.role === 'Admin' ? ' (admin)' : ''}</span>
          <button type="button" onClick={logout}>Sign out</button>
        </div>
      </header>

      <section className="toolbar">
        <input
          type="search"
          placeholder="Book search..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          aria-label="Search books"
        />
        <select
          value={availability}
          onChange={(e) => setAvailability(e.target.value as AvailabilityFilter)}
          aria-label="Filter by availability"
        >
          <option value="All">All books</option>
          <option value="Available">Available</option>
          <option value="Unavailable">Unavailable</option>
        </select>
        <span className="summary">{summary}</span>
      </section>

      {pageError && <div className="form-error">{pageError}</div>}

      <section className="table-wrapper" aria-busy={isLoading}>
        {isLoading && data.items.length === 0 ? (
          <div className="empty-state">Loading...</div>
        ) : (
          <BookTable
            books={data.items}
            currentUserId={currentUserId}
            isMutating={isMutating}
            onEdit={openEdit}
            onDelete={handleDelete}
            onBorrow={handleBorrow}
            onReturn={handleReturn}
          />
        )}
      </section>

      <Pagination
        page={data.page}
        totalPages={data.totalPages}
        onChange={setPage}
      />

      <button type="button" className="fab" onClick={openAdd} aria-label="Add book">
        + Add Book
      </button>

      <BookFormModal
        isOpen={isModalOpen}
        initialBook={editingBook}
        isSubmitting={isSubmitting}
        errorMessage={modalError}
        onClose={closeModal}
        onSubmit={handleSubmit}
      />
    </div>
  );
}
