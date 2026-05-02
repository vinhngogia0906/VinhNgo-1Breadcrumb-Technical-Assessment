import { expect, test } from '@playwright/test';
import { adminCreds, testCreds, uniqueEmail } from './fixtures/seeded';

test.describe('Authentication', () => {
  test('seeded test user can log in and lands on /library', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(testCreds.email);
    await page.getByLabel(/password/i).fill(testCreds.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(page).toHaveURL(/\/library$/);
    await expect(page.getByRole('heading', { name: /library/i })).toBeVisible();
    // Display name appears both in the header and (with " (you)" suffix) in
    // every row the user owns. Scope to the header banner.
    await expect(page.getByRole('banner').getByText(testCreds.displayName)).toBeVisible();
    // Test user is not an admin → no activity-log link
    await expect(page.getByRole('link', { name: /activity log/i })).toHaveCount(0);
  });

  test('seeded admin sees the activity-log link in the header', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(adminCreds.email);
    await page.getByLabel(/password/i).fill(adminCreds.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(page).toHaveURL(/\/library$/);
    await expect(page.getByRole('link', { name: /activity log/i })).toBeVisible();
    await expect(page.getByText(/library admin \(admin\)/i)).toBeVisible();
  });

  test('register flow: a brand new account can sign in immediately', async ({ page }) => {
    const email = uniqueEmail('register');

    await page.goto('/login');
    await page.getByRole('button', { name: /register/i }).click();
    await page.getByLabel(/email/i).fill(email);
    await page.getByLabel(/display name/i).fill('New Crumb');
    await page.getByLabel(/password/i).fill('NewCrumb123');
    await page.getByRole('button', { name: /^register$/i }).click();

    await expect(page).toHaveURL(/\/library$/);
    await expect(page.getByText('New Crumb')).toBeVisible();
  });

  test('wrong password is rejected with the API error message', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(testCreds.email);
    await page.getByLabel(/password/i).fill('WrongPassword');
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(page.getByText(/invalid email or password/i)).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });
});
