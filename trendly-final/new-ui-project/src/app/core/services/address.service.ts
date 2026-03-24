import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddressReadDto, AddressCreateDto, AddressUpdateDto, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class AddressService {
  private readonly baseUrl = `${environment.apiUrl}/Addresses`;
  constructor(private http: HttpClient) {}

  getMine(page = 1, size = 10): Observable<PagedResult<AddressReadDto>> {
    return this.http.get<PagedResult<AddressReadDto>>(`${this.baseUrl}/mine?page=${page}&size=${size}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getById(id: number): Observable<AddressReadDto> {
    return this.http.get<AddressReadDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  create(dto: AddressCreateDto): Observable<AddressReadDto> {
    return this.http.post<AddressReadDto>(this.baseUrl, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  update(id: number, dto: AddressUpdateDto): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/${id}`, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  delete(id: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }
}
