import type { Page, Route } from '@playwright/test';

/**
 * Helpers for mocking the backend API in UI tests.
 *
 * Tests run against `npm run dev` (the Vite dev server) which proxies
 * `/api/*` to the real backend. We intercept those calls with `page.route()`
 * and serve canned JSON instead, so the UI can be exercised in isolation.
 *
 * NOTE on URL matching: route patterns must be anchored to the URL path
 * (regex or absolute URL prefix). A naive glob like `**\/api\/books**` would
 * also match `/src/api/books.ts` — the actual frontend source file Vite
 * serves in dev mode — which would break the JS module load.
 */

export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  role: 'User' | 'Admin';
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

export type Activity = {
  id: string;
  bookId: string;
  bookTitle: string;
  actorId: string;
  actorName: string;
  action: 'Created' | 'Updated' | 'Deleted' | 'Borrowed' | 'Returned';
  details: string | null;
  occurredAt: string;
};

const FIXED_TOKEN = 'mock.jwt.token';

export const adminUser: AuthUser = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'admin@bread.com',
  displayName: 'Library Admin',
  role: 'Admin',
};

export const regularUser: AuthUser = {
  id: '00000000-0000-0000-0000-000000000002',
  email: 'test@bread.com',
  displayName: 'Test Crumb',
  role: 'User',
};

export function makeBook(partial: Partial<Book> & { id: string; title: string; ownerName: string }): Book {
  return {
    ownerId: '00000000-0000-0000-0000-000000000099',
    borrowerId: null,
    borrowerName: null,
    isAvailable: partial.borrowerId == null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...partial,
  };
}

/** Pre-populates the auth state so a page navigation lands authenticated. */
export async function signIn(page: Page, user: AuthUser): Promise<void> {
  await page.addInitScript(
    ([token, snapshot]) => {
      window.localStorage.setItem('library.token', token);
      window.localStorage.setItem('library.user', snapshot);
    },
    [FIXED_TOKEN, JSON.stringify(user)] as const,
  );
}

/** Predicate factory: matches when the URL's pathname matches `re`. */
function pathRe(re: RegExp): (url: URL) => boolean {
  return (url) => re.test(url.pathname);
}

export async function mockAuth(
  page: Page,
  opts: {
    onLogin?: (body: { email: string; password: string }) => Response | Promise<Response>;
    onRegister?: (body: { email: string; displayName: string; password: string }) => Response | Promise<Response>;
  } = {},
): Promise<void> {
  await page.route(pathRe(/^\/api\/auth\/login$/), async (route) => {
    const body = JSON.parse(route.request().postData() ?? '{}');
    const res = (await opts.onLogin?.(body)) ?? defaultLoginResponse(body.email);
    return fulfillFromResponse(route, res);
  });

  await page.route(pathRe(/^\/api\/auth\/register$/), async (route) => {
    const body = JSON.parse(route.request().postData() ?? '{}');
    const res = (await opts.onRegister?.(body)) ?? defaultLoginResponse(body.email);
    return fulfillFromResponse(route, res);
  });
}

function defaultLoginResponse(email: string): Response {
  const user = email === adminUser.email ? adminUser : regularUser;
  return new Response(
    JSON.stringify({
      token: FIXED_TOKEN,
      expiresAt: '2099-01-01T00:00:00Z',
      user,
    }),
    { status: 200, headers: { 'content-type': 'application/json' } },
  );
}

export async function mockBooks(page: Page, books: Book[]): Promise<void> {
  await page.route(pathRe(/^\/api\/books$/), async (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    const url = new URL(route.request().url());
    const q = (url.searchParams.get('search') ?? '').toLowerCase();
    const filter = url.searchParams.get('availability') ?? 'All';
    const page_ = Number(url.searchParams.get('page') ?? '1');
    const pageSize = Number(url.searchParams.get('pageSize') ?? '5');

    let items = books;
    if (q) items = items.filter((b) => b.title.toLowerCase().includes(q));
    if (filter === 'Available') items = items.filter((b) => b.isAvailable);
    if (filter === 'Unavailable') items = items.filter((b) => !b.isAvailable);

    const total = items.length;
    const slice = items.slice((page_ - 1) * pageSize, page_ * pageSize);
    return route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        items: slice,
        page: page_,
        pageSize,
        totalCount: total,
        totalPages: Math.ceil(total / pageSize),
      }),
    });
  });
}

/** Mocks a book mutation matched by the path regex. */
export async function mockBookMutation(
  page: Page,
  pathRegex: RegExp,
  status: number,
  body: unknown,
): Promise<void> {
  await page.route(pathRe(pathRegex), async (route) => {
    return route.fulfill({
      status,
      contentType: 'application/json',
      body: JSON.stringify(body),
    });
  });
}

export async function mockActivity(page: Page, items: Activity[]): Promise<void> {
  await page.route(pathRe(/^\/api\/admin\/activity$/), async (route) => {
    return route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        items,
        page: 1,
        pageSize: 20,
        totalCount: items.length,
        totalPages: items.length === 0 ? 0 : 1,
      }),
    });
  });
}

async function fulfillFromResponse(route: Route, res: Response): Promise<void> {
  const body = await res.text();
  const headers: Record<string, string> = {};
  res.headers.forEach((v, k) => (headers[k] = v));
  await route.fulfill({ status: res.status, headers, body });
}
