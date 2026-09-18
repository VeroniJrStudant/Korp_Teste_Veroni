import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssistantReply, Insight, Invoice, PrintResult } from './models';

@Injectable({ providedIn: 'root' })
export class BillingService {
  private readonly base = '/billing-api/api';

  constructor(private readonly http: HttpClient) {}

  list(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.base}/invoices`);
  }

  get(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.base}/invoices/${id}`);
  }

  create(items: { productId: string; quantity: number }[]): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.base}/invoices`, { items });
  }

  update(id: string, items: { productId: string; quantity: number }[]): Observable<Invoice> {
    return this.http.put<Invoice>(`${this.base}/invoices/${id}`, { items });
  }

  print(id: string): Observable<PrintResult> {
    const headers = new HttpHeaders({ 'Idempotency-Key': `print-${id}` });
    return this.http.post<PrintResult>(`${this.base}/invoices/${id}/print`, {}, { headers });
  }

  health(): Observable<{ service: string; status: string }> {
    return this.http.get<{ service: string; status: string }>(`${this.base}/health`);
  }

  insights(): Observable<Insight[]> {
    return this.http.get<Insight[]>(`${this.base}/ai/insights`);
  }

  ask(question: string): Observable<AssistantReply> {
    return this.http.post<AssistantReply>(`${this.base}/ai/ask`, { question });
  }
}
