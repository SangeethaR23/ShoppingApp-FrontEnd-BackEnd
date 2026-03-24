import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ProductReadDto, ProductCreateDto, ProductUpdateDto,
  PagedResult, PagedRequest, ProductQuery, ReviewReadDto
} from '../models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly baseUrl = `${environment.apiUrl}/Products`;

  constructor(private http: HttpClient) {}

  getPaged(req: PagedRequest): Observable<PagedResult<ProductReadDto>> {
    return this.http.post<PagedResult<ProductReadDto>>(`${this.baseUrl}/paged`, req).pipe(
      catchError(err => throwError(() => err))
    );
  }

  search(query: ProductQuery): Observable<PagedResult<ProductReadDto>> {
    let params = new HttpParams();
    if (query.categoryId != null) params = params.set('categoryId', query.categoryId);
    if (query.nameContains)       params = params.set('nameContains', query.nameContains);
    if (query.priceMin != null)   params = params.set('priceMin', query.priceMin);
    if (query.priceMax != null)   params = params.set('priceMax', query.priceMax);
    if (query.ratingMin != null)  params = params.set('ratingMin', query.ratingMin);
    if (query.sortBy)             params = params.set('sortBy', query.sortBy);
    if (query.sortDir)            params = params.set('sortDir', query.sortDir);
    params = params.set('page', query.page).set('size', query.size);
    return this.http.get<PagedResult<ProductReadDto>>(`${this.baseUrl}/search`, { params }).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getById(id: number): Observable<ProductReadDto> {
    return this.http.get<ProductReadDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  create(dto: ProductCreateDto): Observable<ProductReadDto> {
    return this.http.post<ProductReadDto>(this.baseUrl, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  update(id: number, dto: ProductUpdateDto): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/${id}`, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  delete(id: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  addImage(id: number, url: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/${id}/images`, { url }).pipe(
      catchError(err => throwError(() => err))
    );
  }

  removeImage(id: number, imageId: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${id}/images/${imageId}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  setActive(id: number, isActive: boolean): Observable<unknown> {
    return this.http.patch(`${this.baseUrl}/${id}/active?isActive=${isActive}`, {}).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getReviews(productId: number, page = 1, size = 10): Observable<PagedResult<ReviewReadDto>> {
    return this.http.get<PagedResult<ReviewReadDto>>(
      `${this.baseUrl}/${productId}/reviews?page=${page}&size=${size}`
    ).pipe(catchError(err => throwError(() => err)));
  }
}
