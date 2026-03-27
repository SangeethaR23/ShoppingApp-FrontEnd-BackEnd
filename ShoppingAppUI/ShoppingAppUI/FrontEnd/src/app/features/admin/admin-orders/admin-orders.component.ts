import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderReadDto, ORDER_STATUSES } from '../../../core/models/order.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [RouterLink, PaginationComponent, DatePipe, DecimalPipe, FormsModule],
  templateUrl: './admin-orders.component.html'
})
export class AdminOrdersComponent implements OnInit {
  private orderSvc = inject(OrderService);
  private toast = inject(ToastService);

  orders = signal<OrderReadDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);
  showStatusModal = signal(false);
  statusTarget = signal<OrderReadDto | null>(null);
  newStatus = signal('');
  orderStatuses = ORDER_STATUSES;

  // Filters & sort
  filterStatus = '';
  filterFrom = '';
  filterTo = '';
  filterCustomer = '';
  sortBy = 'date';
  sortDesc = true;

  ngOnInit() { this.load(); }

  load() {
    const req: any = {
      page: this.page(), size: 15,
      sortBy: this.sortBy, desc: this.sortDesc
    };
    if (this.filterStatus) req.status = this.filterStatus;
    if (this.filterFrom) req.from = this.filterFrom;
    if (this.filterTo) req.to = this.filterTo;
    this.orderSvc.getAllOrders(req).subscribe(r => {
      let items = r.items;
      if (this.filterCustomer.trim())
        items = items.filter(o => o.shipToName.toLowerCase().includes(this.filterCustomer.toLowerCase()));
      this.orders.set(items);
      this.totalCount.set(r.totalCount);
      this.totalPages.set(Math.ceil(r.totalCount / 15));
    });
  }

  applyFilters() { this.page.set(1); this.load(); }
  clearFilters() {
    this.filterStatus = ''; this.filterFrom = ''; this.filterTo = ''; this.filterCustomer = '';
    this.sortBy = 'date'; this.sortDesc = true;
    this.page.set(1); this.load();
  }
  toggleSort(col: string) {
    if (this.sortBy === col) this.sortDesc = !this.sortDesc;
    else { this.sortBy = col; this.sortDesc = true; }
    this.page.set(1); this.load();
  }
  sortIcon(col: string) { return this.sortBy === col ? (this.sortDesc ? '↓' : '↑') : '↕'; }

  openStatusModal(order: OrderReadDto) {
    this.statusTarget.set(order); this.newStatus.set(order.status); this.showStatusModal.set(true);
  }
  updateStatus() {
    const order = this.statusTarget()!;
    this.orderSvc.updateStatus(order.id, { status: this.newStatus() }).subscribe(() => {
      this.toast.success('Order status updated'); this.showStatusModal.set(false); this.load();
    });
  }
  onPage(p: number) { this.page.set(p); this.load(); }
  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-info', Shipped: 'badge-primary',
      Delivered: 'badge-success', Cancelled: 'badge-danger',
      ReturnRequested: 'badge-warning', Returned: 'badge-secondary'
    };
    return map[status] ?? 'badge-secondary';
  }
}
