import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/common.models';
import { ReviewReadDto, ReviewCreateDto, ReviewUpdateDto } from '../models/review.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly api = `${environment.apiUrl}/reviews`;

  constructor(private http: HttpClient) {}

  create(dto: ReviewCreateDto): Observable<ReviewReadDto> {
    return this.http.post<ReviewReadDto>(this.api, dto);
  }

  getByProduct(productId: number, page = 1, size = 10): Observable<PagedResult<ReviewReadDto>> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<PagedResult<ReviewReadDto>>(`${this.api}/product/${productId}`, { params });
  }

  getMineForProduct(productId: number): Observable<ReviewReadDto> {
    return this.http.get<ReviewReadDto>(`${this.api}/product/${productId}/mine`);
  }

  update(productId: number, dto: ReviewUpdateDto): Observable<any> {
    return this.http.put(`${this.api}/product/${productId}`, dto);
  }

  delete(productId: number): Observable<any> {
    return this.http.delete(`${this.api}/product/${productId}`);
  }
}
