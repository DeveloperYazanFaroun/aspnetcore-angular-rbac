import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { TokenStore } from './token-store';

const AUTH_ENDPOINTS = ['/api/auth/login', '/api/auth/refresh', '/api/auth/logout'];

/**
 * 1. Adds "Authorization: Bearer <token>" to API calls.
 * 2. On 401 (access token expired): refreshes once and retries the request.
 * 403 is not handled here: the user is signed in but not allowed, so the page shows the message.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenStore = inject(TokenStore);
  const auth = inject(AuthService);

  const isApiCall = request.url.startsWith('/api/');
  const isAuthCall = AUTH_ENDPOINTS.some((url) => request.url.startsWith(url));
  if (!isApiCall || isAuthCall) {
    return next(request);
  }

  return next(withToken(request, tokenStore.token())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refreshAccessToken().pipe(
        catchError((refreshError: unknown) => {
          auth.endSession();
          return throwError(() => refreshError);
        }),
        switchMap((token) => next(withToken(request, token))),
      );
    }),
  );
};

function withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}
