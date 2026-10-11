import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from '../auth/auth.service';
import { TokenStore } from '../auth/token-store';

/**
 * Listens for "PermissionsChanged" from the server. When an administrator changes this user's roles,
 * the UI reloads permissions at once instead of waiting for the next sign-in.
 */
@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly auth = inject(AuthService);
  private readonly tokenStore = inject(TokenStore);
  private connection: HubConnection | null = null;

  connect(): void {
    if (this.connection) {
      return;
    }

    // Browsers can't send headers on WebSocket connections, so SignalR sends the token as ?access_token=.
    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => this.tokenStore.token() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('PermissionsChanged', () =>
      this.auth.loadProfile().subscribe({ error: () => this.auth.endSession() }),
    );
    this.connection.start().catch((error: unknown) => console.warn('Real-time connection failed', error));
  }

  disconnect(): void {
    void this.connection?.stop();
    this.connection = null;
  }
}
