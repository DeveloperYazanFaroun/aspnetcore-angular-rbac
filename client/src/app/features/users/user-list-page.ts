import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, Subject, catchError, of, startWith, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { HasPermissionDirective } from '../../core/auth/has-permission.directive';
import { Permissions } from '../../core/auth/permissions';
import { errorMessage } from '../../core/http/problem-details';
import { RoleApi } from '../roles/role-api';
import { Role } from '../roles/role.models';
import { UserApi } from './user-api';
import { User } from './user.models';

@Component({
  selector: 'app-user-list-page',
  imports: [ReactiveFormsModule, HasPermissionDirective],
  templateUrl: './user-list-page.html',
})
export class UserListPage {
  private readonly api = inject(UserApi);
  private readonly roleApi = inject(RoleApi);
  protected readonly auth = inject(AuthService);

  protected readonly P = Permissions;
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  /** The user whose roles are being edited, and the role ids ticked so far. */
  protected readonly editingUserId = signal<string | null>(null);
  protected readonly editingRoleIds = signal<ReadonlySet<string>>(new Set());

  private readonly reload$ = new Subject<void>();

  protected readonly users = toSignal(
    this.reload$.pipe(
      startWith(undefined),
      switchMap(() => this.api.list().pipe(catchError((error: unknown) => this.fail(error, [] as User[])))),
    ),
    { initialValue: [] as User[] },
  );

  /** Needs roles.view. Without it the role pickers simply stay empty. */
  protected readonly roles = toSignal(this.roleApi.list().pipe(catchError(() => of([] as Role[]))), {
    initialValue: [] as Role[],
  });

  protected readonly createForm = inject(NonNullableFormBuilder).group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected readonly newUserRoleIds = signal<ReadonlySet<string>>(new Set());

  protected isSelf(user: User): boolean {
    return user.id === this.auth.user()?.id;
  }

  protected toggle(target: 'new' | 'edit', roleId: string, checked: boolean): void {
    const state = target === 'new' ? this.newUserRoleIds : this.editingRoleIds;
    state.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(roleId);
      } else {
        next.delete(roleId);
      }
      return next;
    });
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const request = { ...this.createForm.getRawValue(), roleIds: [...this.newUserRoleIds()] };
    this.run(this.api.create(request), `${request.fullName} was created.`, () => {
      this.createForm.reset();
      this.newUserRoleIds.set(new Set());
    });
  }

  protected startEditing(user: User): void {
    this.editingUserId.set(user.id);
    this.editingRoleIds.set(new Set(user.roles.map((role) => role.id)));
  }

  protected saveRoles(user: User): void {
    this.run(this.api.assignRoles(user.id, [...this.editingRoleIds()]), `Roles of ${user.fullName} were updated.`, () =>
      this.editingUserId.set(null),
    );
  }

  protected toggleActive(user: User): void {
    const action = user.isActive ? this.api.deactivate(user.id) : this.api.activate(user.id);
    this.run(action, `${user.fullName} was ${user.isActive ? 'deactivated' : 'activated'}.`);
  }

  private run(action: Observable<unknown>, success: string, onSuccess?: () => void): void {
    this.error.set(null);
    this.notice.set(null);
    action.subscribe({
      next: () => {
        this.notice.set(success);
        onSuccess?.();
        this.reload$.next();
      },
      error: (error: unknown) => this.error.set(errorMessage(error)),
    });
  }

  private fail<T>(error: unknown, fallback: T): Observable<T> {
    this.error.set(errorMessage(error));
    return of(fallback);
  }
}
