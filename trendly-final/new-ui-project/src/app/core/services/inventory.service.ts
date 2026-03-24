import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { InventoryReadDto, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly baseUrl = `${environment.apiUrl}/Inventories`;
  constructor(private http: HttpClient) {}

  getPaged(opts: {
    productId?: number; categoryId?: number; sku?: string;
    lowStockOnly?: boolean; sortBy?: string; desc?: boolean; page?: number; size?: number;
  }): Observable<PagedResult<InventoryReadDto>> {
    let params = new HttpParams();
    if (opts.productId != null)   params = params.set('productId', opts.productId);
    if (opts.categoryId != null)  params = params.set('categoryId', opts.categoryId);
    if (opts.sku)                 params = params.set('sku', opts.sku);
    if (opts.lowStockOnly != null) params = params.set('lowStockOnly', opts.lowStockOnly);
    if (opts.sortBy)              params = params.set('sortBy', opts.sortBy);
    if (opts.desc != null)        params = params.set('desc', opts.desc);
    params = params.set('page', opts.page ?? 1).set('size', opts.size ?? 20);
    return this.http.get<PagedResult<InventoryReadDto>>(this.baseUrl, { params }).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getByProduct(productId: number): Observable<InventoryReadDto> {
    return this.http.get<InventoryReadDto>(`${this.baseUrl}/by-product/${productId}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  adjust(productId: number, delta: number, reason?: string): Observable<InventoryReadDto> {
    return this.http.post<InventoryReadDto>(
      `${this.baseUrl}/by-product/${productId}/adjust`,
      { delta, reason }
    ).pipe(catchError(err => throwError(() => err)));
  }

  setQuantity(productId: number, quantity: number): Observable<InventoryReadDto> {
    return this.http.put<InventoryReadDto>(
      `${this.baseUrl}/by-product/${productId}/set`,
      { quantity }
    ).pipe(catchError(err => throwError(() => err)));
  }

  setReorderLevel(productId: number, reorderLevel: number): Observable<InventoryReadDto> {
    return this.http.post<InventoryReadDto>(
      `${this.baseUrl}/by-product/${productId}/reorder-level`,
      { reorderLevel }
    ).pipe(catchError(err => throwError(() => err)));
  }
}
