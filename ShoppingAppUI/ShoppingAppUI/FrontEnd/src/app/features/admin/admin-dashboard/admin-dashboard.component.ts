import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { OrderService } from '../../../core/services/order.service';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div>
      <h1 class="section-title">Dashboard</h1>
      <div class="stats-grid">
        <div class="stat-card card">
          <div class="stat-icon">📦</div>
          <div class="stat-info">
            <div class="stat-value">{{ totalProducts() }}</div>
            <div class="stat-label">Total Products</div>
          </div>
        </div>
        <div class="stat-card card">
          <div class="stat-icon">🛒</div>
          <div class="stat-info">
            <div class="stat-value">{{ totalOrders() }}</div>
            <div class="stat-label">Total Orders</div>
          </div>
        </div>
        <div class="stat-card card">
          <div class="stat-icon">👥</div>
          <div class="stat-info">
            <div class="stat-value">{{ totalUsers() }}</div>
            <div class="stat-label">Total Users</div>
          </div>
        </div>
        <div class="stat-card card">
          <div class="stat-icon">⏳</div>
          <div class="stat-info">
            <div class="stat-value">{{ pendingOrders() }}</div>
            <div class="stat-label">Pending Orders</div>
          </div>
        </div>
      </div>

      <div class="quick-links">
        <h2 style="font-size:1.1rem;font-weight:700;margin-bottom:1rem">Quick Actions</h2>
        <div class="quick-grid">
          <a routerLink="/admin/products" class="quick-card card">
            <span>📦</span><span>Manage Products</span>
          </a>
          <a routerLink="/admin/orders" class="quick-card card">
            <span>🛒</span><span>Manage Orders</span>
          </a>
          <a routerLink="/admin/users" class="quick-card card">
            <span>👥</span><span>Manage Users</span>
          </a>
          <a routerLink="/admin/inventory" class="quick-card card">
            <span>🏭</span><span>Inventory</span>
          </a>
          <a routerLink="/admin/logs" class="quick-card card">
            <span>📋</span><span>Audit Logs</span>
          </a>
          <a routerLink="/admin/promos" class="quick-card card">
            <span>🎟️</span><span>Promo Codes</span>
          </a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .stats-grid { display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:1.25rem;margin-bottom:2rem; }
    .stat-card { display:flex;align-items:center;gap:1rem;padding:1.5rem; }
    .stat-icon { font-size:2.5rem; }
    .stat-value { font-size:2rem;font-weight:900;color:var(--primary); }
    .stat-label { font-size:0.85rem;color:var(--text-muted);font-weight:600; }
    .quick-grid { display:grid;grid-template-columns:repeat(auto-fill,minmax(160px,1fr));gap:1rem; }
    .quick-card { display:flex;flex-direction:column;align-items:center;gap:0.5rem;padding:1.5rem;text-align:center;font-weight:600;font-size:0.9rem;cursor:pointer;transition:all var(--transition);text-decoration:none;color:var(--text); }
    .quick-card span:first-child { font-size:2rem; }
    .quick-card:hover { transform:translateY(-4px);box-shadow:var(--shadow-lg); }
  `]
})
export class AdminDashboardComponent implements OnInit {
  private productSvc = inject(ProductService);
  private orderSvc = inject(OrderService);
  private userSvc = inject(UserService);

  totalProducts = signal(0);
  totalOrders = signal(0);
  totalUsers = signal(0);
  pendingOrders = signal(0);

  ngOnInit() {
    this.productSvc.getPaged({ page: 1, size: 1 }).subscribe(r => this.totalProducts.set(r.totalCount));
    this.orderSvc.getAllOrders({ page: 1, size: 1 }).subscribe(r => this.totalOrders.set(r.totalCount));
    this.orderSvc.getAllOrders({ page: 1, size: 1, status: 'Pending' }).subscribe(r => this.pendingOrders.set(r.totalCount));
    this.userSvc.getPaged({ page: 1, size: 1 }).subscribe(r => this.totalUsers.set(r.totalCount));
  }
}
