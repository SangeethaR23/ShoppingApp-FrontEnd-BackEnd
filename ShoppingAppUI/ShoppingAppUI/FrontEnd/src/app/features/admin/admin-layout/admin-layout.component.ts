import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.css']
})
export class AdminLayoutComponent {
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);
  collapsed = signal(false);

  navItems = [
    { label: 'Dashboard', icon: '📊', route: '/admin' },
    { label: 'Products', icon: '📦', route: '/admin/products' },
    { label: 'Categories', icon: '🏷️', route: '/admin/categories' },
    { label: 'Inventory', icon: '🏭', route: '/admin/inventory' },
    { label: 'Orders', icon: '🛒', route: '/admin/orders' },
    { label: 'Users', icon: '👥', route: '/admin/users' },
    { label: 'Promo Codes', icon: '🎟️', route: '/admin/promos' },
    { label: 'Audit Logs', icon: '📋', route: '/admin/logs' },
  ];

  logout() {
    this.auth.logout();
    this.toast.success('Logged out');
    this.router.navigate(['/auth/login']);
  }
}
