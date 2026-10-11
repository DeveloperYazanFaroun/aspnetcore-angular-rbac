import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, take } from 'rxjs';
import { AuthService } from './auth.service';

/** Only signed-in users. Others go to the login page and come back afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  return inject(AuthService).isAuthenticated$.pipe(
    take(1),
    map((signedIn) => signedIn || router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })),
  );
};

/** Only signed-out users (the login page). */
export const guestGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(AuthService).isAuthenticated$.pipe(
    take(1),
    map((signedIn) => !signedIn || router.createUrlTree(['/'])),
  );
};

/**
 * Requires every permission listed in the route data:
 * { canActivate: [permissionGuard], data: { permissions: [Permissions.Roles.View] } }
 */
export const permissionGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const required = (route.data['permissions'] ?? []) as readonly string[];

  return inject(AuthService)
    .hasAll$(...required)
    .pipe(
      take(1),
      map((allowed) => allowed || router.createUrlTree(['/forbidden'])),
    );
};
