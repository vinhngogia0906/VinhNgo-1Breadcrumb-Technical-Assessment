import { api } from './client';
import type { BookActivity, PagedResult } from './types';

export type ActivityQuery = {
  page?: number;
  pageSize?: number;
};

export const adminApi = {
  activity: (params: ActivityQuery) =>
    api.get<PagedResult<BookActivity>>('/admin/activity', { params })
      .then((r) => r.data),
};
