import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import type { UserRole } from '../api/types';
import { useAuth } from './useAuth';

type Props = {
  role: UserRole;
  children: ReactNode;
};

/**
 * Renders children only if the authenticated user has the required role.
 * Sends authenticated-but-wrong-role users to /library and unauthenticated
 * users to /login.
 */
export function RequireRole({ role, children }: Props) {
  const { user, isAuthenticated, isInitialized } = useAuth();
  if (!isInitialized) return null;
  if (!isAuthenticated || !user) return <Navigate to="/login" replace />;
  if (user.role !== role) return <Navigate to="/library" replace />;
  return <>{children}</>;
}
