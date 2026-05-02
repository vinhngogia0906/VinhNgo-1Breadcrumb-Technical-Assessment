/**
 * Constants matching the seeded credentials in
 * `src/backend/LibraryApi.Infrastructure/Data/DataSeeder.cs`.
 */
export const adminCreds = {
  email: 'admin@bread.com',
  password: 'AdminPass123',
  displayName: 'Library Admin',
};

export const testCreds = {
  email: 'test@bread.com',
  password: 'TestPass123',
  displayName: 'Test Crumb',
};

/**
 * Generate a unique email so tests can register / interact without colliding
 * with each other on a long-lived database.
 */
export function uniqueEmail(prefix = 'e2e'): string {
  const stamp = Date.now();
  const rand = Math.random().toString(36).slice(2, 8);
  return `${prefix}+${stamp}.${rand}@bread.com`;
}
