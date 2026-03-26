export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface LogEntryReadDto {
  id: number;
  level: string;
  message: string;
  exception?: string;
  stackTrace?: string;
  source?: string;
  eventId?: number;
  correlationId?: string;
  requestPath?: string;
  createdUtc: string;
}

export interface LogQueryDto {
  level?: string;
  search?: string;
  from?: string;
  to?: string;
  source?: string;
  page: number;
  size: number;
  sortDir?: string;
  sortBy?: string;
}

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
}
