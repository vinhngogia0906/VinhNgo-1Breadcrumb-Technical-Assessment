import { expect, test, type Page } from '@playwright/test';
import { adminCreds, testCreds, uniqueEmail } from './fixtures/seeded';

async function signInWith(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill(password);
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page).toHaveURL(/\/library$/);
}

async function registerAs(page: Page, displayName: string): Promise<string> {
  const email = uniqueEmail('book');
  await page.goto('/login');
  await page.getByRole('button', { name: /register/i }).click();
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/display name/i).fill(displayName);
  await page.getByLabel(/password/i).fill('Password123');
  await page.getByRole('button', { name: /^register$/i }).click();
  await expect(page).toHaveURL(/\/library$/);
  return email;
}

test.describe('Book lifecycle', () => {
  test('register, add, edit, then delete a book', async ({ page }) => {
    await registerAs(page, 'Book Owner');

    const title = `E2E Book ${Date.now()}`;
    await page.getByRole('button', { name: /add book/i }).click();

    const dialog = page.getByRole('dialog', { name: /add a book/i });
    await dialog.getByLabel(/book title/i).fill(title);
    await dialog.getByRole('button', { name: /add book/i }).click();

    await expect(dialog).toBeHidden();
    // Filter to the new book (the demo seed + earlier tests share the DB).
    await page.getByPlaceholder(/book search/i).fill(title);
    const newRow = page.getByRole('row', { name: new RegExp(title) });
    await expect(newRow).toBeVisible();

    // Edit
    await newRow.getByRole('button', { name: /^edit$/i }).click();
    const editDialog = page.getByRole('dialog', { name: /edit book/i });
    await editDialog.getByLabel(/book title/i).fill(`${title} (rev)`);
    await editDialog.getByRole('button', { name: /save changes/i }).click();
    await expect(editDialog).toBeHidden();
    await expect(page.getByRole('row', { name: new RegExp(`${title} \\(rev\\)`) })).toBeVisible();

    // Delete
    page.once('dialog', (d) => d.accept());
    await page
      .getByRole('row', { name: new RegExp(`${title} \\(rev\\)`) })
      .getByRole('button', { name: /^delete$/i })
      .click();
    await expect(page.getByRole('row', { name: new RegExp(`${title} \\(rev\\)`) })).toHaveCount(0);
  });

  test('admin can borrow a book the test user owns and return it', async ({ page }) => {
    // Seed a fresh book owned by the test user so the test is independent.
    await signInWith(page, testCreds.email, testCreds.password);

    const title = `Borrowable ${Date.now()}`;
    await page.getByRole('button', { name: /add book/i }).click();
    const addDialog = page.getByRole('dialog', { name: /add a book/i });
    await addDialog.getByLabel(/book title/i).fill(title);
    await addDialog.getByRole('button', { name: /add book/i }).click();
    await expect(addDialog).toBeHidden();

    // Sign out, sign in as admin
    await page.getByRole('button', { name: /sign out/i }).click();
    await expect(page).toHaveURL(/\/login$/);

    await signInWith(page, adminCreds.email, adminCreds.password);
    await page.getByPlaceholder(/book search/i).fill(title);

    const row = page.getByRole('row', { name: new RegExp(title) });
    await expect(row).toBeVisible();

    await row.getByRole('button', { name: /^borrow$/i }).click();
    await expect(
      page.getByRole('row', { name: new RegExp(title) }).locator('.badge-unavailable'),
    ).toContainText(/borrowed by you/i);

    await page
      .getByRole('row', { name: new RegExp(title) })
      .getByRole('button', { name: /^return$/i })
      .click();
    await expect(
      page.getByRole('row', { name: new RegExp(title) }).locator('.badge-available'),
    ).toBeVisible();
  });

  test('search filters the seeded demo books', async ({ page }) => {
    await signInWith(page, testCreds.email, testCreds.password);

    await page.getByPlaceholder(/book search/i).fill('Pragmatic');
    await expect(page.getByRole('cell', { name: /pragmatic programmer/i })).toBeVisible();
    await expect(page.getByRole('cell', { name: /clean code/i })).toHaveCount(0);
  });
});
