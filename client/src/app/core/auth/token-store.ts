import { Injectable, signal } from '@angular/core';

/**
 * Holds the access token in memory only.
 * Not in localStorage: any script on the page can read localStorage, so one XSS bug would leak the token.
 * After a page reload the token is restored with the HttpOnly refresh cookie.
 */
@Injectable({ providedIn: 'root' })
export class TokenStore {
  private readonly accessToken = signal<string | null>(null);

  readonly token = this.accessToken.asReadonly();

  set(token: string): void {
    this.accessToken.set(token);
  }

  clear(): void {
    this.accessToken.set(null);
  }
}
