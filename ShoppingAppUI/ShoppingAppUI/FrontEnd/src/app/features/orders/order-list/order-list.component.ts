import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderSummaryDto } from '../../../core/models/order.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [RouterLink, PaginationComponent, ConfirmDialogComponent, DatePipe, DecimalPipe],
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

  ngOnInit() { this.load(); }

  load() {
    this.orderSvc.getMyOrders({ page: this.page(), size: 10, sortBy: 'date', desc: true }).subscribe(r => {
      this.orders.set(r.items);
      this.totalPages.set(Math.ceil(r.totalCount / 10));
    });
  }

  onPage(p: number) { this.page.set(p); this.load(); }

  canCancel(status: string) {
    return ['Pending', 'Confirmed'].includes(status);
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
