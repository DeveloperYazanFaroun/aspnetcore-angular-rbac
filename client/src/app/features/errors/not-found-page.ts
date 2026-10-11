import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  template: `
    <main class="flex min-h-screen items-center justify-center px-4">
      <section class="card max-w-lg text-center">
        <p class="text-sm font-semibold text-slate-500">404 · Not found</p>
        <h1 class="mt-2 text-xl font-semibold">This page doesn't exist</h1>
        <a routerLink="/" class="btn btn-primary mt-6">Back to home</a>
      </section>
    </main>
  `,
})
export class NotFoundPage {}
