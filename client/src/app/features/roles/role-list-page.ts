import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { HasPermissionDirective } from '../../core/auth/has-permission.directive';
import { Permissions } from '../../core/auth/permissions';
import { errorMessage } from '../../core/http/problem-details';
import { RoleApi } from './role-api';
import { Role } from './role.models';

@Component({
  selector: 'app-role-list-page',
  imports: [RouterLink, HasPermissionDirective],
  templateUrl: './role-list-page.html',
})
export class RoleListPage {
  protected readonly P = Permissions;
  protected readonly error = signal<string | null>(null);

  protected readonly roles = toSignal(
    inject(RoleApi)
      .list()
      .pipe(
        catchError((error: unknown) => {
          this.error.set(errorMessage(error));
          return of([] as Role[]);
        }),
      ),
    { initialValue: [] as Role[] },
  );
}
