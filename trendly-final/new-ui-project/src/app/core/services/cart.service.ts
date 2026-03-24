import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CartReadDto, CartAddItemDto, CartUpdateItemDto } from '../models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly baseUrl = `${environment.apiUrl}/Cart`;

  readonly cart = signal<CartReadDto | null>(null);
  readonly itemCount = computed(() => this.cart()?.items.reduce((s, i) => s + i.quantity, 0) ?? 0);
  readonly subTotal = computed(() => this.cart()?.subTotal ?? 0);

  constructor(private http: HttpClient) {}

  loadCart(): Observable<CartReadDto> {
    return this.http.get<CartReadDto>(`${this.baseUrl}/me`).pipe(
      tap(c => this.cart.set(c)),
      catchError(err => throwError(() => err))
    );
  }

  addItem(dto: CartAddItemDto): Observable<CartReadDto> {
    return this.http.post<CartReadDto>(`${this.baseUrl}/items`, dto).pipe(
      tap(c => this.cart.set(c)),
      catchError(err => throwError(() => err))
    );
  }

  updateItem(dto: CartUpdateItemDto): Observable<CartReadDto> {
    return this.http.put<CartReadDto>(`${this.baseUrl}/items`, dto).pipe(
      tap(c => this.cart.set(c)),
      catchError(err => throwError(() => err))
    );
  }

  removeItem(productId: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/items/${productId}`).pipe(
      tap(() => this.loadCart().subscribe()),
      catchError(err => throwError(() => err))
    );
  }

  clearCart(): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/items`).pipe(
      tap(() => this.cart.set(null)),
      catchError(err => throwError(() => err))
    );
  }

  resetCart(): void {
    this.cart.set(null);
  }
}
