import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderReadDto } from '../../../core/models';

export const ORDER_STATUSES = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [FormsModule, DecimalPipe, DatePipe, RouterLink],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css'
})
export class AdminOrdersComponent implements OnInit {
  private orderSvc = inject(OrderService);
  private toast    = inject(ToastService);

  orders      = signal<OrderReadDto[]>([]);
  loading     = signal(true);
  page        = signal(1);
  totalCount  = signal(0);
  totalPages  = computed(() => Math.ceil(this.totalCount() / 15));
  filterStatus = '';
  filterFrom   = '';
  filterTo     = '';
  statuses     = ORDER_STATUSES;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.orderSvc.getAllPaged({
      page: this.page(), size: 15,
      sortBy: 'date', desc: true,
      status: this.filterStatus || undefined,
      from:   this.filterFrom   || undefined,
      to:     this.filterTo     || undefined
    }).subscribe({
      next: r => {
        this.orders.set(r.items);
        this.totalCount.set(r.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  goToPage(pg: number): void { this.page.set(pg); this.load(); }

  clearFilters(): void {
    this.filterStatus = '';
    this.filterFrom   = '';
    this.filterTo     = '';
    this.page.set(1);
    this.load();
  }

  updateStatus(id: number, status: string): void {
    this.orderSvc.updateStatus(id, { status }).subscribe({
      next: () => {
        this.orders.update(list => list.map(o => o.id === id ? { ...o, status } : o));
        this.toast.success(`Order status updated to "${status}".`);
      },
      error: () => {}
    });
  }
}
