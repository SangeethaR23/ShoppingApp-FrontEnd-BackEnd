import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

// Angular 21: guards use functional form (unchanged), but
// AuthService.isLoggedIn is now a linkedSignal derived from currentUser.
export const authGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (!auth.isLoggedIn()) { router.navigate(['/login']); return false; }
  // Admins should not access user-facing protected routes — redirect to admin panel
  if (auth.isAdmin()) { router.navigate(['/admin']); return false; }
  return true;
};

export const roleGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isAdmin()) return true;
  router.navigate(['/']);
  return false;
};
