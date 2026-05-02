import { api } from './client';
import type { AvailabilityFilter, Book, PagedResult } from './types';

export type BookSearchParams = {
  search?: string;
  availability?: AvailabilityFilter;
  page?: number;
  pageSize?: number;
};

// Path conventions match the OpenAPI contract published at /openapi/v1.yaml:
//   GET    /books            list (with query filters)
//   POST   /book              create
//   GET    /book/{id}         single
//   PUT    /book/{id}         update
//   DELETE /book/{id}         delete
//   POST   /book/{id}/borrow  borrow
//   POST   /book/{id}/return  return
export const booksApi = {
  search: (params: BookSearchParams) =>
    api.get<PagedResult<Book>>('/books', { params }).then((r) => r.data),
  create: (title: string) =>
    api.post<Book>('/book', { title }).then((r) => r.data),
  update: (id: string, title: string) =>
    api.put<Book>(`/book/${id}`, { title }).then((r) => r.data),
  remove: (id: string) =>
    api.delete<void>(`/book/${id}`).then((r) => r.data),
  borrow: (id: string) =>
    api.post<Book>(`/book/${id}/borrow`).then((r) => r.data),
  return: (id: string) =>
    api.post<Book>(`/book/${id}/return`).then((r) => r.data),
};
