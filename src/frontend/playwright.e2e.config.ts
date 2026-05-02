import { defineConfig, devices } from '@playwright/test';

/**
 * E2E tests: run against the full docker-compose stack
 * (frontend + backend + postgres). Set E2E_BASE_URL to the frontend URL.
 *
 * Local: `docker compose up -d --build` then `npm run test:e2e`.
 * CI:    workflow brings the stack up, then runs this suite.
 */
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:8080';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false, // E2E shares one DB; serialize for stable assertions
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report-e2e' }]],

  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
