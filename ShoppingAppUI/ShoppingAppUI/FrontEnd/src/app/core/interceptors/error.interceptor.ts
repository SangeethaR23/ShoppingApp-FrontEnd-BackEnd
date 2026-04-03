import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // err.error may be a raw string when Content-Type is application/problem+json
      const body = typeof err.error === 'string' ? tryParse(err.error) : err.error;

      if (err.status === 401) {
        const msg = body?.detail || body?.title || 'Unauthorized.';
        // Only logout + redirect if the user was already authenticated
        if (auth.isLoggedIn()) {
          auth.logout();
          router.navigate(['/auth/login']);
          toast.error('Session expired. Please login again.');
        } else {
          toast.error(msg);
        }
      } else if (err.status === 403) {
        toast.error('Access denied. You do not have permission.');
        router.navigate(['/forbidden']);
      } else if (err.status === 0) {
        toast.error('Cannot connect to server. Please try again.');
      } else {
        const msg = body?.detail || body?.title || body?.message;
        // console.error("body")
        // console.error(body)
        toast.error(msg);
      }
      return throwError(() => err);
    })
  );
};

function tryParse(str: string): any {
  try { return JSON.parse(str); } catch { return null; }
}
