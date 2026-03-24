import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css'
})
export class CartComponent implements OnInit {
  readonly cartSvc = inject(CartService);
  private toast = inject(ToastService);
  private router = inject(Router);

  ngOnInit(): void {
    this.cartSvc.loadCart().subscribe({ error: () => {} });
  }

  updateQty(productId: number, qty: number): void {
    if (qty < 1) { this.removeItem(productId); return; }
    this.cartSvc.updateItem({ productId, quantity: qty }).subscribe({ error: () => {} });
  }

  removeItem(productId: number): void {
    this.cartSvc.removeItem(productId).subscribe({
      next: () => this.toast.info('Item removed from cart.'),
      error: () => {}
    });
  }

  clearCart(): void {
    this.cartSvc.clearCart().subscribe({
      next: () => this.toast.info('Cart cleared.'),
      error: () => {}
    });
  }

  decQty(productId: number, current: number): void {
    this.updateQty(productId, current - 1);
  }

  incQty(productId: number, current: number): void {
    this.updateQty(productId, current + 1);
  }

  stars(avg: number): string {
    const full = Math.round(avg);
    return '★'.repeat(full) + '☆'.repeat(5 - full);
  }
}
