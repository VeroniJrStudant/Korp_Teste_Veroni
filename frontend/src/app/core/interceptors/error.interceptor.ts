import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const silent = req.headers.has('X-Silent-Error') || req.url.includes('/health');
      if (!silent) {
        const detail = err.error?.detail || err.error?.title || err.message || 'Falha de comunicação.';
        toast.error(detail);
      }
      return throwError(() => err);
    })
  );
};
