import { api } from './client';
import type { AvailabilityFilter, Book, PagedResult } from './types';

export type BookSearchParams = {
  search?: string;
  availability?: AvailabilityFilter;
  page?: number;
  pageSize?: number;
};

export const booksApi = {
  search: (params: BookSearchParams) =>
    api.get<PagedResult<Book>>('/books', { params }).then((r) => r.data),
  create: (title: string) =>
    api.post<Book>('/books', { title }).then((r) => r.data),
  update: (id: string, title: string) =>
    api.put<Book>(`/books/${id}`, { title }).then((r) => r.data),
  remove: (id: string) =>
    api.delete<void>(`/books/${id}`).then((r) => r.data),
  borrow: (id: string) =>
    api.post<Book>(`/books/${id}/borrow`).then((r) => r.data),
  return: (id: string) =>
    api.post<Book>(`/books/${id}/return`).then((r) => r.data),
};
