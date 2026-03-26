import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { WishlistService } from '../../core/services/wishlist.service';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';
import { signal } from '@angular/core';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [RouterLink, ConfirmDialogComponent, DecimalPipe],
  templateUrl: './wishlist.component.html',
  styleUrls: ['./wishlist.component.css']
})
export class WishlistComponent implements OnInit {
  wishlistSvc = inject(WishlistService);
  private cartSvc = inject(CartService);
  private toast = inject(ToastService);
  private router = inject(Router);

  removeTarget = signal<number | null>(null);
  showConfirm = signal(false);

  ngOnInit() { this.wishlistSvc.load().subscribe(); }

  moveToCart(productId: number) {
    this.wishlistSvc.moveToCart(productId).subscribe(() => {
      this.cartSvc.loadCart().subscribe();
      this.toast.success('Moved to cart');
    });
  }

  confirmRemove(productId: number) { this.removeTarget.set(productId); this.showConfirm.set(true); }

  doRemove() {
    const pid = this.removeTarget();
    if (!pid) return;
    this.wishlistSvc.toggle({ productId: pid }).subscribe(() => this.toast.info('Removed from wishlist'));
    this.showConfirm.set(false);
  }
}
