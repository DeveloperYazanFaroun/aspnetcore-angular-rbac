import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { distinctUntilChanged, map } from 'rxjs';
import { AuthService } from './core/auth/auth.service';
import { NotificationsService } from './core/realtime/notifications.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {
  constructor() {
    const notifications = inject(NotificationsService);

    // Keep the real-time connection open while someone is signed in.
    inject(AuthService)
      .user$.pipe(
        map((user) => user !== null),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((signedIn) => (signedIn ? notifications.connect() : notifications.disconnect()));
  }
}
