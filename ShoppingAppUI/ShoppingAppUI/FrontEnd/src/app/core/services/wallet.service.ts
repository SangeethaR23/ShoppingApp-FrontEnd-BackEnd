import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface WalletSummary {
  id: number;
  userId: number;
  balance: number;
}

export interface WalletTransaction {
  id: number;
  amount: number;
  type: string;
  reference: string | null;
  remarks: string | null;
  createdUtc: string;
}

export interface WalletTransactionsPage {
  items: WalletTransaction[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class WalletService {
  private readonly api = `${environment.apiUrl}/wallet`;
  private _balance = signal<number>(0);
  readonly balance = this._balance.asReadonly();

  constructor(private http: HttpClient) {}

  getMyWallet(): Observable<WalletSummary> {
    return this.http.get<WalletSummary>(`${this.api}/me`).pipe(
      tap(w => this._balance.set(w.balance))
    );
  }

  getTransactions(page = 1, size = 10): Observable<WalletTransactionsPage> {
    return this.http.get<WalletTransactionsPage>(
      `${this.api}/me/transactions?page=${page}&size=${size}`
    );
  }

  credit(amount: number): Observable<{ balance: number; message: string }> {
    return this.http.post<{ balance: number; message: string }>(
      `${this.api}/me/credit`, { amount }
    ).pipe(tap(r => this._balance.set(r.balance)));
  }
}
