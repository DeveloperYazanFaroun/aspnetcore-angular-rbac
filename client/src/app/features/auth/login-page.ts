import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { errorMessage } from '../../core/http/problem-details';

interface DemoAccount {
  email: string;
  roles: string;
}

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Bound from the query string (?returnUrl=...). */
  readonly returnUrl = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly demoPassword = 'Passw0rd!';
  protected readonly demoAccounts: DemoAccount[] = [
    { email: 'admin@contoso.test', roles: 'Administrator' },
    { email: 'accountant@contoso.test', roles: 'Accountant' },
    { email: 'approver@contoso.test', roles: 'Approver' },
    { email: 'lead@contoso.test', roles: 'Accountant + Approver' },
    { email: 'viewer@contoso.test', roles: 'Viewer' },
    { email: 'accountant@fabrikam.test', roles: 'Accountant (other tenant)' },
  ];

  protected useDemoAccount(account: DemoAccount): void {
    this.form.setValue({ email: account.email, password: this.demoPassword });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.error.set(null);

    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigateByUrl(this.safeReturnUrl()),
      error: (error: unknown) => {
        this.error.set(errorMessage(error));
        this.submitting.set(false);
      },
    });
  }

  /** Only allow in-app paths, never a full URL to another site (open redirect). */
  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/';
  }
}
