import { DestroyRef, Directive, TemplateRef, ViewContainerRef, inject, input } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, switchMap } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Renders the element only when the user has the permission (or every permission in a list).
 *
 *   <button *appHasPermission="P.Invoices.Approve">Approve</button>
 *   <a *appHasPermission="[P.Invoices.View, P.Invoices.Export]">Export</a>
 *
 * This is UX only. The API checks the same permission on every request.
 */
@Directive({ selector: '[appHasPermission]' })
export class HasPermissionDirective {
  readonly appHasPermission = input.required<string | readonly string[]>();

  constructor() {
    const templateRef = inject(TemplateRef<unknown>);
    const viewContainer = inject(ViewContainerRef);
    const auth = inject(AuthService);

    toObservable(this.appHasPermission)
      .pipe(
        switchMap((required) => auth.hasAll$(...(typeof required === 'string' ? [required] : required))),
        distinctUntilChanged(),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((allowed) => {
        viewContainer.clear();
        if (allowed) {
          viewContainer.createEmbeddedView(templateRef);
        }
      });
  }
}
