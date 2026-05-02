import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminApi } from '../api/admin';
import { readErrorMessage } from '../api/client';
import type { BookActivity, BookAction, PagedResult } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { Pagination } from '../components/Pagination';

const PAGE_SIZE = 20;

const empty: PagedResult<BookActivity> = {
  items: [],
  page: 1,
  pageSize: PAGE_SIZE,
  totalCount: 0,
  totalPages: 0,
};

const actionBadgeClass: Record<BookAction, string> = {
  Created: 'badge badge-action-created',
  Updated: 'badge badge-action-updated',
  Deleted: 'badge badge-action-deleted',
  Borrowed: 'badge badge-action-borrowed',
  Returned: 'badge badge-action-returned',
};

function formatTimestamp(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString();
}

export function AdminActivityPage() {
  const { user, logout } = useAuth();
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<BookActivity>>(empty);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await adminApi.activity({ page, pageSize: PAGE_SIZE });
      setData(result);
      if (result.items.length === 0 && result.page > 1 && result.totalPages > 0) {
        setPage(result.totalPages);
      }
    } catch (err) {
      setError(readErrorMessage(err, 'Failed to load activity log'));
    } finally {
      setIsLoading(false);
    }
  }, [page]);

  useEffect(() => {
    reload();
  }, [reload]);

  return (
    <div className="library-page">
      <header className="page-header">
        <div>
          <h1>Activity log</h1>
          <p className="muted">All borrow / return / edit events across the library</p>
        </div>
        <div className="user-info">
          <Link to="/library" className="header-link">Back to library</Link>
          <span>{user?.displayName} (admin)</span>
          <button type="button" onClick={logout}>Sign out</button>
        </div>
      </header>

      <section className="toolbar">
        <button type="button" onClick={reload} disabled={isLoading}>
          {isLoading ? 'Refreshing…' : 'Refresh'}
        </button>
        <span className="summary">
          {data.totalCount === 0
            ? 'No activity yet'
            : `${data.totalCount} event${data.totalCount === 1 ? '' : 's'}`}
        </span>
      </section>

      {error && <div className="form-error">{error}</div>}

      <section className="table-wrapper" aria-busy={isLoading}>
        {data.items.length === 0 && !isLoading ? (
          <div className="empty-state">
            Once Crumbs start borrowing and returning books, their actions will show up here.
          </div>
        ) : (
          <table className="book-table">
            <thead>
              <tr>
                <th>When</th>
                <th>Actor</th>
                <th>Action</th>
                <th>Book</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((entry) => (
                <tr key={entry.id}>
                  <td>{formatTimestamp(entry.occurredAt)}</td>
                  <td>{entry.actorName}</td>
                  <td>
                    <span className={actionBadgeClass[entry.action] ?? 'badge'}>
                      {entry.action}
                    </span>
                  </td>
                  <td className="cell-title">{entry.bookTitle}</td>
                  <td className="muted">{entry.details ?? ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <Pagination
        page={data.page}
        totalPages={data.totalPages}
        onChange={setPage}
      />
    </div>
  );
}
