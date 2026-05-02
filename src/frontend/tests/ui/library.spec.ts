import { expect, test } from '@playwright/test';
import {
  adminUser,
  makeBook,
  mockBookMutation,
  mockBooks,
  regularUser,
  signIn,
} from './fixtures/mockApi';

test.describe('Library page', () => {
  test('renders books with owner and availability badges', async ({ page }) => {
    await signIn(page, regularUser);
    await mockBooks(page, [
      makeBook({
        id: 'b1',
        title: 'Clean Code',
        ownerId: regularUser.id,
        ownerName: regularUser.displayName,
      }),
      makeBook({
        id: 'b2',
        title: 'DDIA',
        ownerId: 'someone-else',
        ownerName: 'Alice',
        borrowerId: regularUser.id,
        borrowerName: regularUser.displayName,
        isAvailable: false,
      }),
    ]);

    await page.goto('/library');

    await expect(page.getByRole('cell', { name: 'Clean Code' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'DDIA' })).toBeVisible();
    await expect(page.getByText(/test crumb \(you\)/i)).toBeVisible();
    await expect(page.locator('.badge-available')).toBeVisible();
    await expect(page.locator('.badge-unavailable')).toContainText(/borrowed by you/i);
  });

  test('search input filters the table', async ({ page }) => {
    await signIn(page, regularUser);
    await mockBooks(page, [
      makeBook({ id: 'b1', title: 'Clean Code', ownerName: 'Alice' }),
      makeBook({ id: 'b2', title: 'Refactoring', ownerName: 'Alice' }),
    ]);

    await page.goto('/library');
    await expect(page.getByRole('cell', { name: 'Clean Code' })).toBeVisible();

    await page.getByPlaceholder(/book search/i).fill('refactoring');
    await expect(page.getByRole('cell', { name: 'Refactoring' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Clean Code' })).toHaveCount(0);
  });

  test('clicking borrow on an available book triggers the API call and reload', async ({ page }) => {
    await signIn(page, regularUser);

    const book = makeBook({
      id: 'b1',
      title: 'The Pragmatic Programmer',
      ownerId: 'someone-else',
      ownerName: 'Alice',
    });

    await mockBooks(page, [book]);
    await mockBookMutation(page, /^\/api\/book\/b1\/borrow$/, 200, {
      ...book,
      borrowerId: regularUser.id,
      borrowerName: regularUser.displayName,
      isAvailable: false,
    });

    await page.goto('/library');
    await expect(page.getByRole('cell', { name: book.title })).toBeVisible();

    // Swap the books mock to return the borrowed state on reload.
    // page.unroute needs the same URL matcher; use a path predicate.
    await page.unrouteAll({ behavior: 'wait' });
    await mockBookMutation(page, /^\/api\/book\/b1\/borrow$/, 200, {
      ...book,
      borrowerId: regularUser.id,
      borrowerName: regularUser.displayName,
      isAvailable: false,
    });
    await mockBooks(page, [
      {
        ...book,
        borrowerId: regularUser.id,
        borrowerName: regularUser.displayName,
        isAvailable: false,
      },
    ]);

    const borrowButton = page.getByRole('button', { name: /^borrow$/i });
    await expect(borrowButton).toBeEnabled();
    await borrowButton.click();

    await expect(page.getByText(/borrowed by you/i)).toBeVisible();
  });

  test('owner sees edit and delete buttons; non-owners do not', async ({ page }) => {
    await signIn(page, regularUser);
    await mockBooks(page, [
      makeBook({
        id: 'mine',
        title: 'I own this',
        ownerId: regularUser.id,
        ownerName: regularUser.displayName,
      }),
      makeBook({
        id: 'theirs',
        title: 'Someone else owns this',
        ownerId: 'other',
        ownerName: 'Alice',
      }),
    ]);

    await page.goto('/library');

    const myRow = page.getByRole('row', { name: /i own this/i });
    await expect(myRow.getByRole('button', { name: /^edit$/i })).toBeEnabled();
    await expect(myRow.getByRole('button', { name: /^delete$/i })).toBeEnabled();

    const theirRow = page.getByRole('row', { name: /someone else owns this/i });
    await expect(theirRow.getByRole('button', { name: /only the owner can edit/i })).toBeDisabled();
    await expect(theirRow.getByRole('button', { name: /only the owner can delete/i })).toBeDisabled();
  });

  test('admin link is visible only when role is Admin', async ({ page }) => {
    await mockBooks(page, []);

    await signIn(page, regularUser);
    await page.goto('/library');
    await expect(page.getByRole('link', { name: /activity log/i })).toHaveCount(0);

    await page.context().clearCookies();
    await page.evaluate(() => window.localStorage.clear());

    await signIn(page, adminUser);
    await page.goto('/library');
    await expect(page.getByRole('link', { name: /activity log/i })).toBeVisible();
  });

  test('Add Book modal opens, validates, and submits', async ({ page }) => {
    await signIn(page, regularUser);
    await mockBooks(page, []);

    let createdTitle: string | null = null;
    await page.route(
      (url) => url.pathname === '/api/book',
      async (route) => {
        if (route.request().method() !== 'POST') return route.fallback();
        const body = JSON.parse(route.request().postData() ?? '{}');
        createdTitle = body.title;
        return route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'new',
            title: body.title,
            ownerId: regularUser.id,
            ownerName: regularUser.displayName,
            borrowerId: null,
            borrowerName: null,
            isAvailable: true,
            createdAt: '2026-01-01T00:00:00Z',
            updatedAt: '2026-01-01T00:00:00Z',
          }),
        });
      },
    );

    await page.goto('/library');
    await page.getByRole('button', { name: /add book/i }).click();

    const dialog = page.getByRole('dialog', { name: /add a book/i });
    await expect(dialog).toBeVisible();

    const submit = dialog.getByRole('button', { name: /add book/i });
    await expect(submit).toBeDisabled();

    await dialog.getByLabel(/book title/i).fill('Refactoring');
    await expect(submit).toBeEnabled();
    await submit.click();

    await expect(dialog).toBeHidden();
    expect(createdTitle).toBe('Refactoring');
  });
});
