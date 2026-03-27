import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderReadDto } from '../../../core/models/order.models';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [RouterLink, ConfirmDialogComponent, DatePipe, DecimalPipe],
  templateUrl: './order-detail.component.html',
  styleUrls: ['./order-detail.component.css']
})
export class OrderDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private orderSvc = inject(OrderService);
  private toast = inject(ToastService);

  order = signal<OrderReadDto | null>(null);
  showCancelConfirm = signal(false);
  showReturnConfirm = signal(false);

  readonly trackingSteps = ['Pending', 'Confirmed', 'Shipped', 'Delivered'];

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.orderSvc.getById(id).subscribe(o => this.order.set(o));
  }

  canCancel() { return ['Pending', 'Confirmed'].includes(this.order()?.status ?? ''); }
  canReturn() { return this.order()?.status === 'Delivered'; }

  doCancel() {
    this.orderSvc.cancelOrder(this.order()!.id, 'Cancelled by user').subscribe(r => {
      this.toast.success(r.message);
      this.orderSvc.getById(this.order()!.id).subscribe(o => this.order.set(o));
    });
    this.showCancelConfirm.set(false);
  }

  doReturn() {
    this.orderSvc.returnOrder(this.order()!.id, 'Return requested by user').subscribe(() => {
      this.toast.success('Return request submitted');
      this.orderSvc.getById(this.order()!.id).subscribe(o => this.order.set(o));
    });
    this.showReturnConfirm.set(false);
  }

  getStepStatus(step: string): 'done' | 'current' | 'pending' {
    const status = this.order()?.status ?? '';
    const cancelled = ['Cancelled', 'ReturnRequested', 'ReturnApproved', 'ReturnRejected', 'Returned'].includes(status);
    if (cancelled) return step === 'Pending' ? 'done' : 'pending';
    const idx = this.trackingSteps.indexOf(step);
    const curIdx = this.trackingSteps.indexOf(status);
    if (idx < curIdx) return 'done';
    if (idx === curIdx) return 'current';
    return 'pending';
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-info', Shipped: 'badge-primary',
      Delivered: 'badge-success', Cancelled: 'badge-danger',
      ReturnRequested: 'badge-warning', Returned: 'badge-secondary'
    };
    return map[status] ?? 'badge-secondary';
  }

  stepIcon(step: string): string {
    const icons: Record<string, string> = { Pending: '🕐', Confirmed: '✓', Shipped: '🚚', Delivered: '📦' };
    return icons[step] ?? '•';
  }
}
