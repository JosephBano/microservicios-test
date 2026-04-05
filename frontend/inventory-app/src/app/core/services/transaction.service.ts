import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Transaction, CreateTransactionRequest, UpdateTransactionRequest, TransactionFilter } from '../models/transaction.model';
import { PagedResponse } from '../models/paged-response.model';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.transactionServiceUrl}/transactions`;

  getAll(filter: TransactionFilter = {}): Observable<PagedResponse<Transaction>> {
    let params = new HttpParams();
    if (filter.productId) params = params.set('productId', filter.productId);
    if (filter.type) params = params.set('type', filter.type);
    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom);
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo);
    params = params.set('page', filter.page ?? 1);
    params = params.set('pageSize', filter.pageSize ?? 10);
    return this.http.get<PagedResponse<Transaction>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.baseUrl}/${id}`);
  }

  getByProduct(productId: string, page = 1, pageSize = 10): Observable<PagedResponse<Transaction>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<Transaction>>(`${this.baseUrl}/product/${productId}`, { params });
  }

  create(transaction: CreateTransactionRequest): Observable<Transaction> {
    return this.http.post<Transaction>(this.baseUrl, transaction);
  }

  update(id: string, transaction: UpdateTransactionRequest): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.baseUrl}/${id}`, transaction);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
