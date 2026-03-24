import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { LoadingService } from '../services/loading.service';
import { ToastService } from '../services/toast.service';

export const appInterceptor: HttpInterceptorFn = (req, next) => {
  const auth    = inject(AuthService);
  const loading = inject(LoadingService);
  const toast   = inject(ToastService);
  const router  = inject(Router);

  loading.show();

  // Attach JWT token
  const token = auth.getToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    finalize(() => loading.hide()),
    catchError((err: HttpErrorResponse) => {
      switch (err.status) {
        case 401:
          auth.logout();
          router.navigate(['/login']);
          break;
        case 403:
          toast.error('Access denied. You do not have permission.');
          router.navigate(['/']);
          break;
        case 500:
          toast.error('Server error. Please try again later.');
          break;
        default: {
          const msg = err.error?.message ?? err.error?.title ?? err.message ?? 'An error occurred.';
          toast.error(msg);
        }
      }
      return throwError(() => err);
    })
  );
};
