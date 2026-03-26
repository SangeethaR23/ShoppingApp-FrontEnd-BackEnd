import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/common.models';
import { InventoryReadDto, InventoryAdjustRequestDto, InventorySetRequestDto, InventoryReorderLevelRequestDto } from '../models/inventory.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly api = `${environment.apiUrl}/inventories`;

  constructor(private http: HttpClient) {}

  getPaged(opts: {
    productId?: number; categoryId?: number; sku?: string;
    lowStockOnly?: boolean; sortBy?: string; desc?: boolean; page?: number; size?: number;
  }): Observable<PagedResult<InventoryReadDto>> {
    let params = new HttpParams();
    if (opts.productId) params = params.set('productId', opts.productId);
    if (opts.categoryId) params = params.set('categoryId', opts.categoryId);
    if (opts.sku) params = params.set('sku', opts.sku);
    if (opts.lowStockOnly != null) params = params.set('lowStockOnly', opts.lowStockOnly);
    if (opts.sortBy) params = params.set('sortBy', opts.sortBy);
    if (opts.desc != null) params = params.set('desc', opts.desc);
    params = params.set('page', opts.page ?? 1).set('size', opts.size ?? 10);
    return this.http.get<PagedResult<InventoryReadDto>>(this.api, { params });
  }

  getByProduct(productId: number): Observable<InventoryReadDto> {
    return this.http.get<InventoryReadDto>(`${this.api}/by-product/${productId}`);
  }

  adjust(productId: number, dto: InventoryAdjustRequestDto): Observable<InventoryReadDto> {
    return this.http.post<InventoryReadDto>(`${this.api}/by-product/${productId}/adjust`, dto);
  }

  setQuantity(productId: number, dto: InventorySetRequestDto): Observable<InventoryReadDto> {
    return this.http.put<InventoryReadDto>(`${this.api}/by-product/${productId}/set`, dto);
  }

  setReorderLevel(productId: number, dto: InventoryReorderLevelRequestDto): Observable<InventoryReadDto> {
    return this.http.post<InventoryReadDto>(`${this.api}/by-product/${productId}/reorder-level`, dto);
  }
}
