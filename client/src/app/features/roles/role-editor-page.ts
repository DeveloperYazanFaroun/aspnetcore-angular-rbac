import { Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable, catchError, combineLatest, of, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { Permissions } from '../../core/auth/permissions';
import { errorMessage } from '../../core/http/problem-details';
import { RoleApi } from './role-api';
import { PermissionGroup, Role } from './role.models';

/** Create or edit a role and tick its permissions, grouped by module. */
@Component({
  selector: 'app-role-editor-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './role-editor-page.html',
})
export class RoleEditorPage {
  private readonly api = inject(RoleApi);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  /** Route parameter: a role id, or undefined on /roles/new. */
  readonly id = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
  });

  protected readonly selected = signal<ReadonlySet<string>>(new Set());
  protected readonly error = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly catalogue = toSignal(
    this.api.permissionCatalogue().pipe(catchError(() => of([] as PermissionGroup[]))),
    { initialValue: [] as PermissionGroup[] },
  );

  protected readonly role = toSignal(
    toObservable(this.id).pipe(
      switchMap((id) => (id ? this.api.get(id) : of(null))),
      catchError((error: unknown) => {
        this.error.set(errorMessage(error));
        return of(null);
      }),
    ),
  );

  private readonly canManage = toSignal(this.auth.hasAll$(Permissions.Roles.Manage), { initialValue: false });

  /** System roles and users without roles.manage get a read-only view. */
  protected readonly readOnly = computed(() => !this.canManage() || this.role()?.isSystem === true);
  protected readonly isNew = computed(() => !this.id());

  constructor() {
    combineLatest([toObservable(this.role), toObservable(this.readOnly)])
      .pipe(takeUntilDestroyed())
      .subscribe(([role, readOnly]) => {
      if (role) {
        this.form.setValue({ name: role.name, description: role.description ?? '' });
        this.selected.set(new Set(role.permissions));
      }
      if (readOnly) {
        this.form.disable({ emitEvent: false });
      } else {
        this.form.enable({ emitEvent: false });
      }
    });
  }

  protected toggle(code: string, checked: boolean): void {
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(code);
      } else {
        next.delete(code);
      }
      return next;
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description } = this.form.getRawValue();
    const request = { name, description: description || null, permissions: [...this.selected()] };
    const id = this.id();
    const action: Observable<Role> = id ? this.api.update(id, request) : this.api.create(request);
    this.execute(action);
  }

  protected remove(): void {
    const id = this.id();
    if (id) {
      this.execute(this.api.delete(id));
    }
  }

  private execute(action: Observable<unknown>): void {
    this.saving.set(true);
    this.error.set(null);
    action.subscribe({
      next: () => void this.router.navigate(['/roles']),
      error: (error: unknown) => {
        this.error.set(errorMessage(error));
        this.saving.set(false);
      },
    });
  }
}
