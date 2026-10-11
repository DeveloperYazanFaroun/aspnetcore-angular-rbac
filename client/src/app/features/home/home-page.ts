import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { HasPermissionDirective } from '../../core/auth/has-permission.directive';
import { Permissions } from '../../core/auth/permissions';

/** Shows who you are and what you can do: a live view of your effective permissions. */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink, HasPermissionDirective],
  templateUrl: './home-page.html',
})
export class HomePage {
  protected readonly auth = inject(AuthService);
  protected readonly P = Permissions;
}
