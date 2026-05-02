export type UserRole = 'User' | 'Admin';

export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
};

export type AuthResponse = {
  token: string;
  expiresAt: string;
  user: AuthUser;
};

export type Book = {
  id: string;
  title: string;
  ownerId: string;
  ownerName: string;
  borrowerId: string | null;
  borrowerName: string | null;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type AvailabilityFilter = 'All' | 'Available' | 'Unavailable';

export type ApiError = {
  error: string;
};

export type BookAction =
  | 'Created'
  | 'Updated'
  | 'Deleted'
  | 'Borrowed'
  | 'Returned';

export type BookActivity = {
  id: string;
  bookId: string;
  bookTitle: string;
  actorId: string;
  actorName: string;
  action: BookAction;
  details: string | null;
  occurredAt: string;
};
