import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PromoReadDto, PromoCreateDto, ApplyPromoDto } from '../models/promo.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PromoService {
  private readonly api = `${environment.apiUrl}/promo`;

  constructor(private http: HttpClient) {}

  create(dto: PromoCreateDto): Observable<PromoReadDto> {
    return this.http.post<PromoReadDto>(`${this.api}/create`, dto);
  }

  activate(id: number, active: boolean): Observable<any> {
    return this.http.post(`${this.api}/${id}/activate?active=${active}`, {});
  }

  getAll(): Observable<PromoReadDto[]> {
    return this.http.get<PromoReadDto[]>(`${this.api}/all`);
  }

  apply(dto: ApplyPromoDto): Observable<PromoReadDto> {
    return this.http.post<PromoReadDto>(`${this.api}/apply`, dto);
  }
}
