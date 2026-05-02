export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
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
