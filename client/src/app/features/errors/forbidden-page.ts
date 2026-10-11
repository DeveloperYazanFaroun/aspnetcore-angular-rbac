import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden-page',
  imports: [RouterLink],
  template: `
    <section class="card mx-auto max-w-lg text-center">
      <p class="text-sm font-semibold text-rose-600">403 · Forbidden</p>
      <h1 class="mt-2 text-xl font-semibold">You don't have access to this page</h1>
      <p class="mt-2 text-sm text-slate-500">
        You are signed in, but none of your roles grants the permission this page needs.
        Ask an administrator if you think you should have access.
      </p>
      <a routerLink="/" class="btn btn-primary mt-6">Back to home</a>
    </section>
  `,
})
export class ForbiddenPage {}
