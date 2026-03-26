import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guards admin-only routes.
 * - Unauthenticated → redirect to /auth/login
 * - Authenticated but NOT Admin → redirect to /
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isAdmin()) return true;
  if (!auth.isLoggedIn()) {
    router.navigate(['/auth/login']);
  } else {
    router.navigate(['/']); // User tried to access admin area
  }
  return false;
};

// NOTE: userGuard has been REMOVED.
// Use authGuard (from auth.guard.ts) on all user-accessible protected routes.
// authGuard correctly allows BOTH User and Admin roles through.
