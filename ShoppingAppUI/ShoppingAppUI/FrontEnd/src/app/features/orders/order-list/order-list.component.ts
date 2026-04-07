import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderSummaryDto, ORDER_STATUSES } from '../../../core/models/order.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [RouterLink, PaginationComponent, ConfirmDialogComponent, DatePipe, DecimalPipe, FormsModule],
  templateUrl: './order-list.component.html',
  styleUrls: ['./order-list.component.css']
})
export class OrderListComponent implements OnInit {
  private orderSvc = inject(OrderService);
  private toast = inject(ToastService);
  private router = inject(Router);

  orders = signal<OrderSummaryDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  cancelTarget = signal<number | null>(null);
  showConfirm = signal(false);

  // Filters
  statusFilter = '';
  fromDate = '';
  toDate = '';
  readonly statuses = ['', ...ORDER_STATUSES];

  ngOnInit() { this.load(); }

  load() {
    const req: any = { page: this.page(), size: 10, sortBy: 'date', desc: true };
    if (this.statusFilter) req.status = this.statusFilter;
    if (this.fromDate) req.from = this.fromDate;
    if (this.toDate) req.to = this.toDate;
    this.orderSvc.getMyOrders(req).subscribe(r => {
      this.orders.set(r.items);
      this.totalPages.set(Math.ceil(r.totalCount / 10));
    });
  }

  applyFilters() { this.page.set(1); this.load(); }

  clearFilters() {
    this.statusFilter = '';
    this.fromDate = '';
    this.toDate = '';
    this.page.set(1);
    this.load();
  }

  onPage(p: number) { this.page.set(p); this.load(); }

  canCancel(order: OrderSummaryDto) {
    if (!['Pending', 'Confirmed'].includes(order.status)) return false;
    const days = (Date.now() - new Date(order.placedAtUtc).getTime()) / (1000 * 60 * 60 * 24);
    return days <= 3;
  }

  confirmCancel(id: number) { this.cancelTarget.set(id); this.showConfirm.set(true); }

  doCancel() {
    const id = this.cancelTarget();
    if (!id) return;
    this.orderSvc.cancelOrder(id, 'Cancelled by user').subscribe(r => {
      this.toast.success(r.message);
      this.load();
    });
    this.showConfirm.set(false);
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-info', Shipped: 'badge-primary',
      Delivered: 'badge-success', Cancelled: 'badge-danger',
      ReturnRequested: 'badge-warning', Returned: 'badge-secondary'
    };
    return map[status] ?? 'badge-secondary';
  }
}
