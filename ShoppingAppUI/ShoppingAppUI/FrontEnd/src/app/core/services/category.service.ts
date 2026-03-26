import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/common.models';
import { CategoryReadDto, CategoryCreateDto, CategoryUpdateDto } from '../models/category.models';
import { PagedRequestDto } from '../models/product.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly api = `${environment.apiUrl}/categories`;

  constructor(private http: HttpClient) {}

  getPaged(req: PagedRequestDto): Observable<PagedResult<CategoryReadDto>> {
    return this.http.post<PagedResult<CategoryReadDto>>(`${this.api}/paged`, req);
  }

  getById(id: number): Observable<CategoryReadDto> {
    return this.http.get<CategoryReadDto>(`${this.api}/${id}`);
  }

  create(dto: CategoryCreateDto): Observable<CategoryReadDto> {
    return this.http.post<CategoryReadDto>(this.api, dto);
  }

  update(id: number, dto: CategoryUpdateDto): Observable<any> {
    return this.http.put(`${this.api}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.api}/${id}`);
  }
}
