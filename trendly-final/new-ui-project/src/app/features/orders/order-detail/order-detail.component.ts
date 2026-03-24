import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderReadDto } from '../../../core/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [RouterLink, DecimalPipe, DatePipe, ConfirmDialogComponent],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.css'
})
export class OrderDetailComponent implements OnInit {
  private route    = inject(ActivatedRoute);
  private orderSvc = inject(OrderService);
  private toast    = inject(ToastService);

  order       = signal<OrderReadDto | null>(null);
  loading     = signal(true);
  cancelling  = signal(false);
  showConfirm = signal(false);

  get canCancel(): boolean {
    const s = this.order()?.status;
    return !!s && !['Shipped', 'Delivered', 'Cancelled'].includes(s);
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.orderSvc.getById(id).subscribe({
      next: o => { this.order.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  cancelOrder(): void {
    this.showConfirm.set(true);
  }

  onCancelConfirmed(): void {
    this.showConfirm.set(false);
    if (!this.order() || this.cancelling()) return;
    this.cancelling.set(true);
    this.orderSvc.cancelOrder(this.order()!.id).subscribe({
      next: res => {
        this.order.update(o => o ? { ...o, status: res.status } : o);
        this.toast.success(res.message);
        this.cancelling.set(false);
      },
      error: () => this.cancelling.set(false)
    });
  }

  onCancelDismissed(): void {
    this.showConfirm.set(false);
  }
}