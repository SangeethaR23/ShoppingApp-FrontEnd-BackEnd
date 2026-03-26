import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { WishlistReadDto, WishlistToggleDto } from '../models/wishlist.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  private readonly api = `${environment.apiUrl}/wishlist`;
  private _items = signal<WishlistReadDto[]>([]);

  readonly items = this._items.asReadonly();

  constructor(private http: HttpClient) {}

  load(): Observable<WishlistReadDto[]> {
    return this.http.get<WishlistReadDto[]>(this.api).pipe(
      tap(items => this._items.set(items))
    );
  }

  toggle(dto: WishlistToggleDto): Observable<{ added: boolean; message: string }> {
    return this.http.post<{ added: boolean; message: string }>(`${this.api}/toggle`, dto).pipe(
      tap(() => this.load().subscribe())
    );
  }

  moveToCart(productId: number): Observable<any> {
    return this.http.post(`${this.api}/move-to-cart/${productId}`, {}).pipe(
      tap(() => this.load().subscribe())
    );
  }

  isInWishlist(productId: number): boolean {
    return this._items().some(i => i.productId === productId);
  }

  clearLocal(): void {
    this._items.set([]);
  }
}
