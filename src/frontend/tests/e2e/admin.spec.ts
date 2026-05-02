import { expect, test, type Page } from '@playwright/test';
import { adminCreds, testCreds } from './fixtures/seeded';

async function signInWith(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill(password);
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page).toHaveURL(/\/library$/);
}

test.describe('Admin activity log', () => {
  test('admin sees activity entries after a borrow + return', async ({ page }) => {
    // 1) test user adds a book
    await signInWith(page, testCreds.email, testCreds.password);
    const title = `Activity ${Date.now()}`;
    await page.getByRole('button', { name: /add book/i }).click();
    const dialog = page.getByRole('dialog', { name: /add a book/i });
    await dialog.getByLabel(/book title/i).fill(title);
    await dialog.getByRole('button', { name: /add book/i }).click();
    await expect(dialog).toBeHidden();

    // 2) sign out, sign in as admin, borrow + return that book
    await page.getByRole('button', { name: /sign out/i }).click();
    await signInWith(page, adminCreds.email, adminCreds.password);
    await page.getByPlaceholder(/book search/i).fill(title);
    const row = page.getByRole('row', { name: new RegExp(title) });
    await row.getByRole('button', { name: /^borrow$/i }).click();
    await expect(
      page.getByRole('row', { name: new RegExp(title) }).locator('.badge-unavailable'),
    ).toBeVisible();

    await page
      .getByRole('row', { name: new RegExp(title) })
      .getByRole('button', { name: /^return$/i })
      .click();
    await expect(
      page.getByRole('row', { name: new RegExp(title) }).locator('.badge-available'),
    ).toBeVisible();

    // 3) navigate to /admin/activity and assert the actions are present
    await page.getByRole('link', { name: /activity log/i }).click();
    await expect(page).toHaveURL(/\/admin\/activity$/);
    await expect(page.getByRole('heading', { name: /activity log/i })).toBeVisible();

    // The most recent two activity rows should be Returned then Borrowed for our book.
    const titleCells = page.getByRole('cell', { name: title });
    await expect(titleCells.first()).toBeVisible();
    await expect(page.locator('.badge-action-borrowed').first()).toBeVisible();
    await expect(page.locator('.badge-action-returned').first()).toBeVisible();
  });

  test('non-admin gets bounced back to /library when trying /admin/activity', async ({ page }) => {
    await signInWith(page, testCreds.email, testCreds.password);
    await page.goto('/admin/activity');
    await expect(page).toHaveURL(/\/library$/);
  });
});
