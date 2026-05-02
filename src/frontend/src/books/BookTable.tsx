import type { Book } from '../api/types';

type Props = {
  books: Book[];
  currentUserId: string;
  isMutating: boolean;
  onEdit: (book: Book) => void;
  onDelete: (book: Book) => void;
  onBorrow: (book: Book) => void;
  onReturn: (book: Book) => void;
};

export function BookTable({
  books,
  currentUserId,
  isMutating,
  onEdit,
  onDelete,
  onBorrow,
  onReturn,
}: Props) {
  if (books.length === 0) {
    return <div className="empty-state">No books found. Add the first one!</div>;
  }

  return (
    <table className="book-table">
      <thead>
        <tr>
          <th>Book</th>
          <th>Owner</th>
          <th>Availability</th>
          <th aria-label="Actions" />
        </tr>
      </thead>
      <tbody>
        {books.map((book) => {
          const isOwner = book.ownerId === currentUserId;
          const isBorrower = book.borrowerId === currentUserId;
          const canBorrow = book.isAvailable && !isOwner;
          const canReturn = !book.isAvailable && (isBorrower || isOwner);

          return (
            <tr key={book.id}>
              <td className="cell-title">{book.title}</td>
              <td>{isOwner ? `${book.ownerName} (you)` : book.ownerName}</td>
              <td>
                {book.isAvailable ? (
                  <span className="badge badge-available">Available</span>
                ) : (
                  <span className="badge badge-unavailable">
                    Borrowed{book.borrowerName ? ` by ${isBorrower ? 'you' : book.borrowerName}` : ''}
                  </span>
                )}
              </td>
              <td className="cell-actions">
                <button
                  type="button"
                  className="icon-button"
                  title={
                    book.isAvailable
                      ? isOwner
                        ? "You can't borrow your own book"
                        : 'Borrow'
                      : canReturn
                      ? 'Return'
                      : 'Borrowed by someone else'
                  }
                  disabled={
                    isMutating ||
                    (book.isAvailable ? !canBorrow : !canReturn)
                  }
                  onClick={() => (book.isAvailable ? onBorrow(book) : onReturn(book))}
                >
                  {book.isAvailable ? '📥' : '↩️'}
                </button>
                <button
                  type="button"
                  className="icon-button"
                  title={isOwner ? 'Edit' : 'Only the owner can edit'}
                  disabled={!isOwner || isMutating}
                  onClick={() => onEdit(book)}
                >
                  ✏️
                </button>
                <button
                  type="button"
                  className="icon-button danger"
                  title={isOwner ? 'Delete' : 'Only the owner can delete'}
                  disabled={!isOwner || isMutating}
                  onClick={() => onDelete(book)}
                >
                  ✖
                </button>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
