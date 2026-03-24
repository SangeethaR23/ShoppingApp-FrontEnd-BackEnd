import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CategoryReadDto, CategoryCreateDto, CategoryUpdateDto,
  PagedResult, PagedRequest
} from '../models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly baseUrl = `${environment.apiUrl}/Categories`;
  constructor(private http: HttpClient) {}

  getPaged(req: PagedRequest): Observable<PagedResult<CategoryReadDto>> {
    return this.http.post<PagedResult<CategoryReadDto>>(`${this.baseUrl}/paged`, req).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getById(id: number): Observable<CategoryReadDto> {
    return this.http.get<CategoryReadDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  create(dto: CategoryCreateDto): Observable<CategoryReadDto> {
    return this.http.post<CategoryReadDto>(this.baseUrl, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  update(id: number, dto: CategoryUpdateDto): Observable<CategoryReadDto> {
    return this.http.put<CategoryReadDto>(`${this.baseUrl}/${id}`, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  delete(id: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }
}
