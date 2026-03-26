import { Component, inject, OnInit, signal, effect } from '@angular/core';
import { RouterOutlet, Router, NavigationStart, NavigationEnd } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';
import { SpinnerComponent } from './shared/spinner/spinner.component';
import { ToastComponent } from './shared/toast/toast.component';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';
import { WishlistService } from './core/services/wishlist.service';
import { filter } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, SpinnerComponent, ToastComponent],
  template: `
    <app-spinner></app-spinner>
    <app-toast></app-toast>
    @if (showNavbar()) {
      <app-navbar></app-navbar>
    }
    <main [class.with-navbar]="showNavbar()">
      <router-outlet></router-outlet>
    </main>
  `,
  styles: [`
    main.with-navbar { min-height: calc(100vh - 64px); }
    main { min-height: 100vh; }
  `]
})
export class AppComponent implements OnInit {
  private auth = inject(AuthService);
  private cart = inject(CartService);
  private wishlist = inject(WishlistService);
  private router = inject(Router);

  showNavbar = signal(true);

  private readonly HIDE_NAVBAR_FOR = ['/admin', '/auth'];

  constructor() {
    // Reactively sync cart & wishlist whenever auth state changes
    effect(() => {
      const loggedIn = this.auth.isLoggedIn();
      const isAdmin = this.auth.isAdmin();
      if (loggedIn && !isAdmin) {
        this.cart.loadCart().subscribe({ error: () => {} });
        this.wishlist.load().subscribe({ error: () => {} });
      } else if (!loggedIn) {
        this.cart.clearLocal();
        this.wishlist.clearLocal();
      }
    });
  }

  ngOnInit() {
    // Use window.location.pathname for reliable initial route detection
    const initPath = window.location.pathname;
    this.showNavbar.set(!this.HIDE_NAVBAR_FOR.some(p => initPath.startsWith(p)));

    // Subscribe to both NavigationStart (immediate hide) and NavigationEnd
    this.router.events.pipe(
      filter(e => e instanceof NavigationStart || e instanceof NavigationEnd)
    ).subscribe((e: any) => {
      const url: string = e.url || e.urlAfterRedirects || '';
      this.showNavbar.set(!this.HIDE_NAVBAR_FOR.some(p => url.startsWith(p)));
    });
  }
}
