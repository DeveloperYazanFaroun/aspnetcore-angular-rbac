import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Turns an API error (RFC 9457 problem details) into a message for the user. */
export function errorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'The server is not reachable. Check that the API is running.';
  }

  const problem = error.error as ProblemDetails | null;
  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }

  return problem?.detail ?? problem?.title ?? fallback;
}
