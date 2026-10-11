import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { HasPermissionDirective } from '../core/auth/has-permission.directive';
import { Permissions } from '../core/auth/permissions';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, HasPermissionDirective],
  templateUrl: './shell.html',
})
export class Shell {
  protected readonly auth = inject(AuthService);
  protected readonly P = Permissions;
}
