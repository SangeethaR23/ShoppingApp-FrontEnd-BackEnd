import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PlaceOrderRequest, PlaceOrderResponse,
  OrderReadDto, OrderSummaryDto,
  CancelOrderResponse, UpdateOrderStatusRequest,
  PagedResult, OrderPagedRequest
} from '../models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly baseUrl = `${environment.apiUrl}/Orders`;
  constructor(private http: HttpClient) {}

  placeOrder(dto: PlaceOrderRequest): Observable<PlaceOrderResponse> {
    return this.http.post<PlaceOrderResponse>(this.baseUrl, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getById(id: number): Observable<OrderReadDto> {
    return this.http.get<OrderReadDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getMyOrders(req: OrderPagedRequest): Observable<PagedResult<OrderSummaryDto>> {
    return this.http.post<PagedResult<OrderSummaryDto>>(`${this.baseUrl}/mine`, req).pipe(
      catchError(err => throwError(() => err))
    );
  }

  cancelOrder(id: number, reason?: string): Observable<CancelOrderResponse> {
    const url = reason
      ? `${this.baseUrl}/${id}/cancel?reason=${encodeURIComponent(reason)}`
      : `${this.baseUrl}/${id}/cancel`;
    return this.http.post<CancelOrderResponse>(url, {}).pipe(
      catchError(err => throwError(() => err))
    );
  }

  // Admin
  getAllPaged(req: OrderPagedRequest): Observable<PagedResult<OrderReadDto>> {
    return this.http.post<PagedResult<OrderReadDto>>(`${this.baseUrl}/paged`, req).pipe(
      catchError(err => throwError(() => err))
    );
  }

  updateStatus(id: number, req: UpdateOrderStatusRequest): Observable<unknown> {
    return this.http.patch(`${this.baseUrl}/${id}/status`, req).pipe(
      catchError(err => throwError(() => err))
    );
  }
}
