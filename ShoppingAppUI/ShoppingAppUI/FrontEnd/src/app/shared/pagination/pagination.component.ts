import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (totalPages > 1) {
      <div class="pagination">
        <button class="page-btn" [disabled]="page <= 1" (click)="go(page - 1)">‹</button>
        @for (p of pages(); track p) {
          <button class="page-btn" [class.active]="p === page" (click)="go(p)">{{ p }}</button>
        }
        <button class="page-btn" [disabled]="page >= totalPages" (click)="go(page + 1)">›</button>
      </div>
    }
  `
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 1;
  @Output() pageChange = new EventEmitter<number>();

  go(p: number) { if (p >= 1 && p <= this.totalPages) this.pageChange.emit(p); }

  pages(): number[] {
    const range: number[] = [];
    const start = Math.max(1, this.page - 2);
    const end = Math.min(this.totalPages, this.page + 2);
    for (let i = start; i <= end; i++) range.push(i);
    return range;
  }
}
