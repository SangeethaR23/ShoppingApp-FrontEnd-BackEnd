import { Component, inject, signal, computed } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent {
  auth = inject(AuthService);
  cart = inject(CartService);
  wishlist = inject(WishlistService);
  private toast = inject(ToastService);
  private router = inject(Router);

  menuOpen = signal(false);

  /** Computed signal — evaluated only when token/user signal changes */
  readonly userName = computed(() => {
    const user = this.auth.currentUser();
    if (!user) return 'Guest';
    const email = user.email ?? '';
    return email.split('@')[0] || 'User';
  });

  /** Wishlist badge count — reactive via signal */
  readonly wishlistCount = computed(() => this.wishlist.items().length);

  toggleMenu() { this.menuOpen.update(v => !v); }
  closeMenu() { this.menuOpen.set(false); }

  logout() {
    this.auth.logout();
    this.cart.clearLocal();
    this.wishlist.clearLocal();
    this.toast.success('Logged out successfully');
    this.router.navigate(['/']);
    this.closeMenu();
  }
}
