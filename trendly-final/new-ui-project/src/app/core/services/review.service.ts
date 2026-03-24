import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ReviewReadDto, ReviewCreateDto, ReviewUpdateDto, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly baseUrl = `${environment.apiUrl}/Reviews`;
  constructor(private http: HttpClient) {}

  create(dto: ReviewCreateDto): Observable<ReviewReadDto> {
    return this.http.post<ReviewReadDto>(this.baseUrl, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getByProduct(productId: number, page = 1, size = 10): Observable<PagedResult<ReviewReadDto>> {
    return this.http.get<PagedResult<ReviewReadDto>>(
      `${this.baseUrl}/product/${productId}?page=${page}&size=${size}`
    ).pipe(catchError(err => throwError(() => err)));
  }

  getMineForProduct(productId: number): Observable<ReviewReadDto> {
    return this.http.get<ReviewReadDto>(`${this.baseUrl}/product/${productId}/mine`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  update(productId: number, dto: ReviewUpdateDto): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/product/${productId}`, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  delete(productId: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/product/${productId}`).pipe(
      catchError(err => throwError(() => err))
    );
  }
}
