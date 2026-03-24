import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  UserProfileReadDto, UpdateUserProfileDto, ChangePasswordDto,
  UserListItemDto, PagedResult, UserPagedRequest
} from '../models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly baseUrl = `${environment.apiUrl}/Users`;
  constructor(private http: HttpClient) {}

  getMe(): Observable<UserProfileReadDto> {
    return this.http.get<UserProfileReadDto>(`${this.baseUrl}/me`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  updateMe(dto: UpdateUserProfileDto): Observable<UserProfileReadDto> {
    return this.http.put<UserProfileReadDto>(`${this.baseUrl}/me`, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  changePassword(dto: ChangePasswordDto): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/me/change-password`, dto).pipe(
      catchError(err => throwError(() => err))
    );
  }

  // Admin
  getPaged(req: UserPagedRequest): Observable<PagedResult<UserListItemDto>> {
    return this.http.post<PagedResult<UserListItemDto>>(`${this.baseUrl}/paged`, req).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getById(id: number): Observable<UserProfileReadDto> {
    return this.http.get<UserProfileReadDto>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  updateRole(id: number, role: string): Observable<unknown> {
    return this.http.patch(`${this.baseUrl}/${id}/role?role=${role}`, {}).pipe(
      catchError(err => throwError(() => err))
    );
  }
}
