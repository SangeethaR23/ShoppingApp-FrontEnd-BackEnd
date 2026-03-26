import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/common.models';
import { ProductReadDto, ProductCreateDto, ProductUpdateDto, ProductQuery, ProductImageCreateDto, PagedRequestDto } from '../models/product.models';
import { ReviewReadDto } from '../models/review.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly api = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  getPaged(req: PagedRequestDto): Observable<PagedResult<ProductReadDto>> {
    return this.http.post<PagedResult<ProductReadDto>>(`${this.api}/paged`, req);
  }

  search(query: ProductQuery): Observable<PagedResult<ProductReadDto>> {
    let params = new HttpParams();
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.includeChildren) params = params.set('includeChildren', query.includeChildren);
    if (query.nameContains) params = params.set('nameContains', query.nameContains);
    if (query.priceMin != null) params = params.set('priceMin', query.priceMin);
    if (query.priceMax != null) params = params.set('priceMax', query.priceMax);
    if (query.ratingMin != null) params = params.set('ratingMin', query.ratingMin);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDir) params = params.set('sortDir', query.sortDir);
    params = params.set('page', query.page).set('size', query.size);
    if (query.inStockOnly) params = params.set('inStockOnly', query.inStockOnly);
    return this.http.get<PagedResult<ProductReadDto>>(`${this.api}/search`, { params });
  }

  getById(id: number): Observable<ProductReadDto> {
    return this.http.get<ProductReadDto>(`${this.api}/${id}`);
  }

  create(dto: ProductCreateDto): Observable<ProductReadDto> {
    return this.http.post<ProductReadDto>(this.api, dto);
  }

  update(id: number, dto: ProductUpdateDto): Observable<any> {
    return this.http.put(`${this.api}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.api}/${id}`);
  }

  addImage(id: number, dto: ProductImageCreateDto): Observable<any> {
    return this.http.post(`${this.api}/${id}/images`, dto);
  }

  removeImage(id: number, imageId: number): Observable<any> {
    return this.http.delete(`${this.api}/${id}/images/${imageId}`);
  }

  setActive(id: number, isActive: boolean): Observable<any> {
    return this.http.patch(`${this.api}/${id}/active?isActive=${isActive}`, {});
  }

  getReviews(id: number, page = 1, size = 10, minRating?: number, sortBy = 'newest', sortDir = 'desc'): Observable<PagedResult<ReviewReadDto>> {
    let params = new HttpParams().set('page', page).set('size', size).set('sortBy', sortBy).set('sortDir', sortDir);
    if (minRating != null) params = params.set('minRating', minRating);
    return this.http.get<PagedResult<ReviewReadDto>>(`${this.api}/${id}/reviews`, { params });
  }
}
