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
      if (err.status === 401) {
        auth.logout();
        router.navigate(['/auth/login']);
        toast.error('Session expired. Please login again.');
      } else if (err.status === 403) {
        toast.error('Access denied. You do not have permission.');
        router.navigate(['/forbidden']);
      } else if (err.status === 0) {
        toast.error('Cannot connect to server. Please try again.');
      } else {
        const msg = err.error?.message || err.error?.detail || err.message || 'Something went wrong.';
        toast.error(msg);
      }
      return throwError(() => err);
    })
  );
};
