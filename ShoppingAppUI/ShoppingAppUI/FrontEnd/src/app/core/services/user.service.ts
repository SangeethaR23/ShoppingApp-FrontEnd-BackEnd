import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/common.models';
import { UserProfileReadDto, UpdateUserProfileDto, ChangePasswordRequestDto, UserListItemDto, UserPagedRequest } from '../models/user.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getMe(): Observable<UserProfileReadDto> {
    return this.http.get<UserProfileReadDto>(`${this.api}/me`);
  }

  updateMe(dto: UpdateUserProfileDto): Observable<UserProfileReadDto> {
    return this.http.put<UserProfileReadDto>(`${this.api}/me`, dto);
  }

  changePassword(dto: ChangePasswordRequestDto): Observable<any> {
    return this.http.post(`${this.api}/me/change-password`, dto);
  }

  // Admin
  getPaged(req: UserPagedRequest): Observable<PagedResult<UserListItemDto>> {
    return this.http.post<PagedResult<UserListItemDto>>(`${this.api}/paged`, req);
  }

  getById(id: number): Observable<UserProfileReadDto> {
    return this.http.get<UserProfileReadDto>(`${this.api}/${id}`);
  }

  updateRole(id: number, role: string): Observable<any> {
    return this.http.patch(`${this.api}/${id}/role?role=${role}`, {});
  }
}
