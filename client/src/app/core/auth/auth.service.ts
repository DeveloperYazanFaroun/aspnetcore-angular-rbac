import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  catchError,
  distinctUntilChanged,
  filter,
  finalize,
  map,
  of,
  shareReplay,
  switchMap,
  tap,
} from 'rxjs';
import { CurrentUser, TokenResponse } from './auth.models';
import { TokenStore } from './token-store';

/**
 * Session state for the whole app: who is signed in and which permissions they have.
 * Exposes RxJS streams for guards and directives, and signals for templates.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenStore = inject(TokenStore);

  /** undefined = not known yet (app starting), null = signed out. */
  private readonly userSubject = new BehaviorSubject<CurrentUser | null | undefined>(undefined);
  private refreshInFlight$: Observable<string> | null = null;

  /** Emits once the session is known, then on every change. */
  readonly user$ = this.userSubject.pipe(
    filter((user): user is CurrentUser | null => user !== undefined),
    distinctUntilChanged(),
  );

  readonly isAuthenticated$ = this.user$.pipe(map((user) => user !== null));

  readonly permissions$: Observable<ReadonlySet<string>> = this.user$.pipe(
    map((user) => new Set(user?.permissions ?? [])),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  readonly user = toSignal(this.user$, { initialValue: null });

  /** True when the user has every permission in the list. */
  hasAll$(...permissions: readonly string[]): Observable<boolean> {
    return this.permissions$.pipe(
      map((granted) => permissions.every((permission) => granted.has(permission))),
      distinctUntilChanged(),
    );
  }

  /** Runs once at startup: uses the refresh cookie (if any) to restore the session after a reload. */
  restoreSession(): Observable<void> {
    return this.refreshAccessToken().pipe(
      switchMap(() => this.loadProfile()),
      map(() => undefined),
      catchError(() => {
        this.clearSession();
        return of(undefined);
      }),
    );
  }

  login(email: string, password: string): Observable<CurrentUser> {
    return this.http.post<TokenResponse>('/api/auth/login', { email, password }).pipe(
      tap((response) => this.tokenStore.set(response.accessToken)),
      switchMap(() => this.loadProfile()),
    );
  }

  /** Reloads profile and permissions, e.g. after the server says they changed. */
  loadProfile(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>('/api/me').pipe(tap((user) => this.userSubject.next(user)));
  }

  /**
   * Gets a new access token with the refresh cookie.
   * Shared, so several requests failing with 401 at the same time cause only one refresh call.
   */
  refreshAccessToken(): Observable<string> {
    this.refreshInFlight$ ??= this.http.post<TokenResponse>('/api/auth/refresh', {}).pipe(
      map((response) => response.accessToken),
      tap((token) => this.tokenStore.set(token)),
      finalize(() => (this.refreshInFlight$ = null)),
      shareReplay(1),
    );
    return this.refreshInFlight$;
  }

  logout(): void {
    this.http
      .post('/api/auth/logout', {})
      .pipe(catchError(() => of(null)))
      .subscribe();
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  /** Called when the session can't be renewed (refresh token expired or revoked). */
  endSession(): void {
    this.clearSession();
    void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
  }

  private clearSession(): void {
    this.tokenStore.clear();
    this.userSubject.next(null);
  }
}
