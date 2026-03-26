import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';
import { environment } from '../../../environments/environment';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject(LoadingService);

  // Only show the spinner for actual API calls, not assets or external resources
  const isApiCall = req.url.startsWith(environment.apiUrl);
  if (!isApiCall) return next(req);

  loading.show();
  return next(req).pipe(finalize(() => loading.hide()));
};
