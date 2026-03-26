import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/common.models';
import {
  OrderReadDto, OrderSummaryDto, PlaceOrderRequestDto, PlaceOrderResponseDto,
  CancelOrderResponseDto, OrderPagedRequest, UpdateOrderStatusRequest
} from '../models/order.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly api = `${environment.apiUrl}/orders`;

  constructor(private http: HttpClient) {}

  placeOrder(dto: PlaceOrderRequestDto): Observable<PlaceOrderResponseDto> {
    return this.http.post<PlaceOrderResponseDto>(this.api, dto);
  }

  getById(id: number): Observable<OrderReadDto> {
    return this.http.get<OrderReadDto>(`${this.api}/${id}`);
  }

  getMyOrders(req: { page: number; size: number; sortBy?: string; desc?: boolean }): Observable<PagedResult<OrderSummaryDto>> {
    return this.http.post<PagedResult<OrderSummaryDto>>(`${this.api}/mine`, req);
  }

  cancelOrder(id: number, reason?: string): Observable<CancelOrderResponseDto> {
    const params = reason ? `?reason=${encodeURIComponent(reason)}` : '';
    return this.http.post<CancelOrderResponseDto>(`${this.api}/${id}/cancel${params}`, {});
  }

  returnOrder(id: number, reason?: string): Observable<CancelOrderResponseDto> {
    const params = reason ? `?reason=${encodeURIComponent(reason)}` : '';
    return this.http.post<CancelOrderResponseDto>(`${this.api}/${id}/return${params}`, {});
  }

  // Admin
  getAllOrders(req: OrderPagedRequest): Observable<PagedResult<OrderReadDto>> {
    return this.http.post<PagedResult<OrderReadDto>>(`${this.api}/paged`, req);
  }

  updateStatus(id: number, req: UpdateOrderStatusRequest): Observable<any> {
    return this.http.patch(`${this.api}/${id}/status`, req);
  }
}
