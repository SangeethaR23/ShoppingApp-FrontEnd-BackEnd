import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/common.models';
import { AddressReadDto, AddressCreateDto, AddressUpdateDto } from '../models/address.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AddressService {
  private readonly api = `${environment.apiUrl}/addresses`;

  constructor(private http: HttpClient) {}

  create(dto: AddressCreateDto): Observable<AddressReadDto> {
    return this.http.post<AddressReadDto>(this.api, dto);
  }

  getById(id: number): Observable<AddressReadDto> {
    return this.http.get<AddressReadDto>(`${this.api}/${id}`);
  }

  getMine(page = 1, size = 20): Observable<PagedResult<AddressReadDto>> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<PagedResult<AddressReadDto>>(`${this.api}/mine`, { params });
  }

  getByUser(userId: number, page = 1, size = 20): Observable<PagedResult<AddressReadDto>> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<PagedResult<AddressReadDto>>(`${this.api}/user/${userId}`, { params });
  }

  update(id: number, dto: AddressUpdateDto): Observable<any> {
    return this.http.put(`${this.api}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.api}/${id}`);
  }
}
