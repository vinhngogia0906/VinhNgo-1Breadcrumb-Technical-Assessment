import { expect, test } from '@playwright/test';
import { adminUser, mockActivity, regularUser, signIn } from './fixtures/mockApi';

test.describe('Admin activity page', () => {
  test('admin sees the activity table with the seeded entries', async ({ page }) => {
    await signIn(page, adminUser);
    await mockActivity(page, [
      {
        id: 'a1',
        bookId: 'b1',
        bookTitle: 'Clean Code',
        actorId: 'u1',
        actorName: 'Alice',
        action: 'Borrowed',
        details: null,
        occurredAt: '2026-05-01T10:00:00Z',
      },
      {
        id: 'a2',
        bookId: 'b1',
        bookTitle: 'Clean Code',
        actorId: 'u1',
        actorName: 'Alice',
        action: 'Returned',
        details: 'Returned from Alice',
        occurredAt: '2026-05-01T11:00:00Z',
      },
    ]);

    await page.goto('/admin/activity');

    await expect(page.getByRole('heading', { name: /activity log/i })).toBeVisible();
    await expect(page.getByText(/2 events/i)).toBeVisible();
    await expect(page.locator('.badge-action-borrowed')).toBeVisible();
    await expect(page.locator('.badge-action-returned')).toBeVisible();
    await expect(page.getByText(/returned from alice/i)).toBeVisible();
  });

  test('non-admin is redirected away from /admin/activity', async ({ page }) => {
    await signIn(page, regularUser);
    await page.route(
      (url) => url.pathname === '/api/books',
      async (route) =>
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [],
            page: 1,
            pageSize: 5,
            totalCount: 0,
            totalPages: 0,
          }),
        }),
    );

    await page.goto('/admin/activity');
    await expect(page).toHaveURL(/\/library$/);
  });

  test('unauthenticated visit redirects to /login', async ({ page }) => {
    await page.goto('/admin/activity');
    await expect(page).toHaveURL(/\/login$/);
  });
});
