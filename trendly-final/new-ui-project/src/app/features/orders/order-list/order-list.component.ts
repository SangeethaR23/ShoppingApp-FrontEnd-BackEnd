import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe, DatePipe, LowerCasePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../core/services/order.service';
import { OrderSummaryDto } from '../../../core/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

export const ORDER_STATUSES = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [RouterLink, DecimalPipe, DatePipe, LowerCasePipe, FormsModule, ConfirmDialogComponent],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.css'
})
export class OrderListComponent implements OnInit {
  private orderSvc = inject(OrderService);

  orders     = signal<OrderSummaryDto[]>([]);
  loading    = signal(true);
  totalCount = signal(0);
  page       = signal(1);
  totalPages = computed(() => Math.ceil(this.totalCount() / 10));

  filterStatus = '';
  filterFrom   = '';
  filterTo     = '';
  sortDesc     = true;
  statuses     = ORDER_STATUSES;

  // Confirm dialog
  showConfirm    = signal(false);
  confirmTitle   = '';
  confirmMessage = '';
  private pendingCancelId: number | null = null;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.orderSvc.getMyOrders({
      page: this.page(), size: 10,
      sortBy: 'date', desc: this.sortDesc,
      status: this.filterStatus || undefined,
      from:   this.filterFrom   || undefined,
      to:     this.filterTo     || undefined
    }).subscribe({
      next: res => {
        this.orders.set(res.items);
        this.totalCount.set(res.totalCount);
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
    this.sortDesc     = true;
    this.page.set(1);
    this.load();
  }

  cancelOrder(id: number): void {
    this.pendingCancelId = id;
    this.confirmTitle   = 'Cancel Order';
    this.confirmMessage = 'Are you sure you want to cancel this order? This action cannot be undone.';
    this.showConfirm.set(true);
  }

  onCancelConfirmed(): void {
    this.showConfirm.set(false);
    if (this.pendingCancelId === null) return;
    const id = this.pendingCancelId;
    this.pendingCancelId = null;
    this.orderSvc.cancelOrder(id).subscribe({
      next: () => {
        this.orders.update(list =>
          list.map(o => o.id === id ? { ...o, status: 'Cancelled' } : o)
        );
      },
      error: () => {}
    });
  }

  onCancelDismissed(): void {
    this.showConfirm.set(false);
    this.pendingCancelId = null;
  }
}
