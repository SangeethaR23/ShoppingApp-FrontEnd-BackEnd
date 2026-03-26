import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { CartReadDto, CartAddItemDto, CartUpdateItemDto } from '../models/cart.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly api = `${environment.apiUrl}/cart`;
  private _cart = signal<CartReadDto | null>(null);

  readonly cart = this._cart.asReadonly();
  readonly cartCount = computed(() => this._cart()?.items?.reduce((s, i) => s + i.quantity, 0) ?? 0);
  readonly cartTotal = computed(() => this._cart()?.subTotal ?? 0);

  constructor(private http: HttpClient) {}

  loadCart(): Observable<CartReadDto> {
    return this.http.get<CartReadDto>(`${this.api}/me`).pipe(
      tap(c => this._cart.set(c))
    );
  }

  addItem(dto: CartAddItemDto): Observable<CartReadDto> {
    return this.http.post<CartReadDto>(`${this.api}/items`, dto).pipe(
      tap(c => this._cart.set(c))
    );
  }

  updateItem(dto: CartUpdateItemDto): Observable<CartReadDto> {
    return this.http.put<CartReadDto>(`${this.api}/items`, dto).pipe(
      tap(c => this._cart.set(c))
    );
  }

  removeItem(productId: number): Observable<any> {
    return this.http.delete(`${this.api}/items/${productId}`).pipe(
      tap(() => this.loadCart().subscribe())
    );
  }

  clearCart(): Observable<any> {
    return this.http.delete(`${this.api}/items`).pipe(
      tap(() => this._cart.set(null))
    );
  }

  clearLocal(): void {
    this._cart.set(null);
  }
}
