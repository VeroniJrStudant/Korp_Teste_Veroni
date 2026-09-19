import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models';

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly base = '/stock-api/api';

  constructor(private readonly http: HttpClient) {}

  list(search = ''): Observable<Product[]> {
    let params = new HttpParams();
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<Product[]>(`${this.base}/products`, { params });
  }

  get(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.base}/products/${id}`);
  }

  create(payload: { code: string; description: string; balance: number }): Observable<Product> {
    return this.http.post<Product>(`${this.base}/products`, payload);
  }

  update(id: string, payload: { description: string; balance: number }): Observable<Product> {
    return this.http.put<Product>(`${this.base}/products/${id}`, payload);
  }

  suggestDescription(code: string, name: string): Observable<{ description: string; source: string }> {
    return this.http.post<{ description: string; source: string }>(`${this.base}/ai/suggest-description`, { code, name });
  }

  health(): Observable<{ service: string; status: string; chaos: boolean }> {
    return this.http.get<{ service: string; status: string; chaos: boolean }>(`${this.base}/health`);
  }

  chaos(enabled: boolean): Observable<{ enabled: boolean; message: string }> {
    return this.http.post<{ enabled: boolean; message: string }>(`${this.base}/chaos`, { enabled });
  }
}
