import { expect, test } from '@playwright/test';
import { adminUser, mockAuth, mockBooks } from './fixtures/mockApi';

test.describe('Login page', () => {
  test('renders the form and toggles between sign-in and register', async ({ page }) => {
    await page.goto('/login');

    await expect(page.getByRole('heading', { name: /library/i })).toBeVisible();
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();

    // Sign-in mode: no display name field
    await expect(page.getByLabel(/display name/i)).toHaveCount(0);

    await page.getByRole('button', { name: /register/i }).click();
    await expect(page.getByLabel(/display name/i)).toBeVisible();

    await page.getByRole('button', { name: /already have an account/i }).click();
    await expect(page.getByLabel(/display name/i)).toHaveCount(0);
  });

  test('submitting valid credentials redirects to /library', async ({ page }) => {
    await mockAuth(page);
    await mockBooks(page, []);

    await page.goto('/login');
    await page.getByLabel(/email/i).fill('admin@bread.com');
    await page.getByLabel(/password/i).fill('AdminPass123');
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(page).toHaveURL(/\/library$/);
    await expect(page.getByRole('heading', { name: /^library$/i })).toBeVisible();
    // Admin badge appears for admin user
    await expect(page.getByText(/library admin \(admin\)/i)).toBeVisible();
  });

  test('shows the API error message when login fails', async ({ page }) => {
    await mockAuth(page, {
      onLogin: () =>
        new Response(JSON.stringify({ error: 'Invalid email or password.' }), {
          status: 400,
          headers: { 'content-type': 'application/json' },
        }),
    });

    await page.goto('/login');
    await page.getByLabel(/email/i).fill('admin@bread.com');
    await page.getByLabel(/password/i).fill('wrongpassword');
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(page.getByText(/invalid email or password/i)).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });
});

test.describe('Login (HTML5 validation)', () => {
  test('email field rejects malformed input before any network call', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill('not-an-email');
    await page.getByLabel(/password/i).fill('whatever');
    await page.getByRole('button', { name: /sign in/i }).click();

    // Native validity API: invalid email blocks submit, URL stays on /login
    await expect(page).toHaveURL(/\/login$/);
    const valid = await page.getByLabel(/email/i).evaluate(
      (el: HTMLInputElement) => el.validity.valid,
    );
    expect(valid).toBe(false);
  });
});

test('unauthenticated visit to /library redirects to /login', async ({ page }) => {
  await page.goto('/library');
  await expect(page).toHaveURL(/\/login$/);
});

test('authenticated user lands on /library directly', async ({ page }) => {
  await mockBooks(page, []);
  // Set localStorage before app boot
  await page.addInitScript(([token, user]) => {
    window.localStorage.setItem('library.token', token);
    window.localStorage.setItem('library.user', user);
  }, ['mock.token', JSON.stringify(adminUser)] as const);

  await page.goto('/');
  await expect(page).toHaveURL(/\/library$/);
});
