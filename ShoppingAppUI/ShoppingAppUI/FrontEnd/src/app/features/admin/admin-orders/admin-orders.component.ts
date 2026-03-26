import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderReadDto } from '../../../core/models/order.models';
import { ORDER_STATUSES } from '../../../core/models/order.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [RouterLink, PaginationComponent, DatePipe, DecimalPipe],
  templateUrl: './admin-orders.component.html'
})
export class AdminOrdersComponent implements OnInit {
  private orderSvc = inject(OrderService);
  private toast = inject(ToastService);

  orders = signal<OrderReadDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);
  filterStatus = signal('');
  showStatusModal = signal(false);
  statusTarget = signal<OrderReadDto | null>(null);
  newStatus = signal('');
  orderStatuses = ORDER_STATUSES;

  ngOnInit() { this.load(); }

  load() {
    this.orderSvc.getAllOrders({
      page: this.page(), size: 15,
      status: this.filterStatus() || undefined,
      sortBy: 'date', desc: true
    }).subscribe(r => {
      this.orders.set(r.items);
      this.totalCount.set(r.totalCount);
      this.totalPages.set(Math.ceil(r.totalCount / 15));
    });
  }

  openStatusModal(order: OrderReadDto) {
    this.statusTarget.set(order);
    this.newStatus.set(order.status);
    this.showStatusModal.set(true);
  }

  updateStatus() {
    const order = this.statusTarget()!;
    this.orderSvc.updateStatus(order.id, { status: this.newStatus() }).subscribe(() => {
      this.toast.success('Order status updated');
      this.showStatusModal.set(false);
      this.load();
    });
  }

  onPage(p: number) { this.page.set(p); this.load(); }
  onFilterChange(val: string) { this.filterStatus.set(val); this.page.set(1); this.load(); }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-info', Shipped: 'badge-primary',
      Delivered: 'badge-success', Cancelled: 'badge-danger',
      ReturnRequested: 'badge-warning', Returned: 'badge-secondary'
    };
    return map[status] ?? 'badge-secondary';
  }
}
