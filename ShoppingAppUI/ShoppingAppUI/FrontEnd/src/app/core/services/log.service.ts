import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, LogEntryReadDto, LogQueryDto } from '../models/common.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LogService {
  private readonly api = `${environment.apiUrl}/admin/logs`;

  constructor(private http: HttpClient) {}

  search(query: LogQueryDto): Observable<PagedResult<LogEntryReadDto>> {
    return this.http.post<PagedResult<LogEntryReadDto>>(`${this.api}/search`, query);
  }
}
