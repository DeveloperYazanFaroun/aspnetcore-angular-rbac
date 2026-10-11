import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Invoice, InvoiceQuery, SaveInvoiceRequest } from './invoice.models';

@Injectable({ providedIn: 'root' })
export class InvoiceApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/invoices';

  list(query: InvoiceQuery): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(this.baseUrl, { params: toParams(query) });
  }

  create(request: SaveInvoiceRequest): Observable<Invoice> {
    return this.http.post<Invoice>(this.baseUrl, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  submit(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/submit`, {});
  }

  approve(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/approve`, {});
  }

  reject(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/reject`, {});
  }

  exportCsv(query: InvoiceQuery): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/export`, { params: toParams(query), responseType: 'blob' });
  }
}

function toParams(query: InvoiceQuery): HttpParams {
  let params = new HttpParams();
  if (query.status) {
    params = params.set('status', query.status);
  }
  if (query.search) {
    params = params.set('search', query.search);
  }
  return params;
}
