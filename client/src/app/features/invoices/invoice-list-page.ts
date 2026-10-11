import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, Subject, catchError, combineLatest, debounceTime, of, startWith, switchMap, tap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { HasPermissionDirective } from '../../core/auth/has-permission.directive';
import { Permissions } from '../../core/auth/permissions';
import { errorMessage } from '../../core/http/problem-details';
import { InvoiceApi } from './invoice-api';
import { INVOICE_STATUSES, Invoice, InvoiceQuery, InvoiceStatus } from './invoice.models';

@Component({
  selector: 'app-invoice-list-page',
  imports: [ReactiveFormsModule, HasPermissionDirective, CurrencyPipe, DatePipe],
  templateUrl: './invoice-list-page.html',
})
export class InvoiceListPage {
  private readonly api = inject(InvoiceApi);
  private readonly auth = inject(AuthService);

  protected readonly P = Permissions;
  protected readonly statuses = INVOICE_STATUSES;

  protected readonly status = signal<InvoiceStatus | ''>('');
  protected readonly search = signal('');
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  private readonly currentUserId = computed(() => this.auth.user()?.id);
  private readonly query = computed<InvoiceQuery>(() => ({ status: this.status(), search: this.search().trim() }));
  private readonly reload$ = new Subject<void>();

  /** Reloads when the filters change (debounced) or after an action. */
  protected readonly invoices = toSignal(
    combineLatest([toObservable(this.query).pipe(debounceTime(250)), this.reload$.pipe(startWith(undefined))]).pipe(
      tap(() => this.loading.set(true)),
      switchMap(([query]) =>
        this.api.list(query).pipe(
          catchError((error: unknown) => {
            this.error.set(errorMessage(error));
            return of([] as Invoice[]);
          }),
        ),
      ),
      tap(() => this.loading.set(false)),
    ),
    { initialValue: [] as Invoice[] },
  );

  protected readonly createForm = inject(NonNullableFormBuilder).group({
    customerName: ['', [Validators.required, Validators.minLength(2)]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
  });

  /** Separation of duties, mirrored in the UI: you can't review an invoice you created. */
  protected isOwnInvoice(invoice: Invoice): boolean {
    return invoice.createdById === this.currentUserId();
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.run(this.api.create(this.createForm.getRawValue()), 'Draft invoice created.', () => this.createForm.reset());
  }

  protected submit(invoice: Invoice): void {
    this.run(this.api.submit(invoice.id), `${invoice.number} was submitted for approval.`);
  }

  protected approve(invoice: Invoice): void {
    this.run(this.api.approve(invoice.id), `${invoice.number} was approved.`);
  }

  protected reject(invoice: Invoice): void {
    this.run(this.api.reject(invoice.id), `${invoice.number} was rejected.`);
  }

  protected remove(invoice: Invoice): void {
    this.run(this.api.delete(invoice.id), `${invoice.number} was deleted.`);
  }

  protected exportCsv(): void {
    this.api.exportCsv(this.query()).subscribe({
      next: (file) => {
        const url = URL.createObjectURL(file);
        const link = Object.assign(document.createElement('a'), { href: url, download: 'invoices.csv' });
        link.click();
        URL.revokeObjectURL(url);
      },
      error: (error: unknown) => this.error.set(errorMessage(error)),
    });
  }

  protected statusClass(status: InvoiceStatus): string {
    switch (status) {
      case 'Draft':
        return 'bg-slate-100 text-slate-700';
      case 'Submitted':
        return 'bg-amber-50 text-amber-700';
      case 'Approved':
        return 'bg-emerald-50 text-emerald-700';
      case 'Rejected':
        return 'bg-rose-50 text-rose-700';
    }
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
}
