import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { LogService } from '../../../core/services/log.service';
import { ToastService } from '../../../core/services/toast.service';
import { LogEntryReadDto } from '../../../core/models/common.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

@Component({
  selector: 'app-admin-logs',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent, DatePipe],
  templateUrl: './admin-logs.component.html'
})
export class AdminLogsComponent implements OnInit {
  private logSvc = inject(LogService);
  private fb = inject(FormBuilder);

  logs = signal<LogEntryReadDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);
  selectedLog = signal<LogEntryReadDto | null>(null);

  filterForm = this.fb.group({
    level: [''],
    search: [''],
    source: [''],
    from: [''],
    to: [''],
    sortDir: ['desc']
  });

  ngOnInit() { this.load(); }

  load() {
    const val = this.filterForm.value;
    this.logSvc.search({
      level: val.level || undefined,
      search: val.search || undefined,
      source: val.source || undefined,
      from: val.from || undefined,
      to: val.to || undefined,
      sortDir: val.sortDir ?? 'desc',
      sortBy: 'date',
      page: this.page(),
      size: 20
    }).subscribe(r => {
      this.logs.set(r.items);
      this.totalCount.set(r.totalCount);
      this.totalPages.set(Math.ceil(r.totalCount / 20));
    });
  }

  onPage(p: number) { this.page.set(p); this.load(); }
  applyFilters() { this.page.set(1); this.load(); }

  levelClass(level: string): string {
    return { Error: 'badge-danger', Warning: 'badge-warning', Info: 'badge-info' }[level] ?? 'badge-secondary';
  }
}
