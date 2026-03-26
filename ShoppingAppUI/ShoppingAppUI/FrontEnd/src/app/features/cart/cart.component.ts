import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [RouterLink, ConfirmDialogComponent, DecimalPipe],
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.css']
})
export class CartComponent implements OnInit {
  cartSvc = inject(CartService);
  private toast = inject(ToastService);
  private router = inject(Router);

  removeTarget = signal<number | null>(null);
  showConfirm = signal(false);

  ngOnInit() {
    this.cartSvc.loadCart().subscribe();
  }

  updateQty(productId: number, qty: number) {
    if (qty < 1) { this.confirmRemove(productId); return; }
    this.cartSvc.updateItem({ productId, quantity: qty }).subscribe();
  }

  confirmRemove(productId: number) {
    this.removeTarget.set(productId);
    this.showConfirm.set(true);
  }

  doRemove() {
    const pid = this.removeTarget();
    if (pid == null) return;
    this.cartSvc.removeItem(pid).subscribe(() => this.toast.success('Item removed'));
    this.showConfirm.set(false);
  }

  checkout() {
    if (!this.cartSvc.cart()?.items?.length) { this.toast.warning('Your cart is empty'); return; }
    this.router.navigate(['/checkout']);
  }
}
