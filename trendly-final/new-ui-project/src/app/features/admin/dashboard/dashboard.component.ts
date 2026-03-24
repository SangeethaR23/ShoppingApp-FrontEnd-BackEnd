import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { ProductService } from '../../../core/services/product.service';
import { OrderService } from '../../../core/services/order.service';
import { UserService } from '../../../core/services/user.service';
import { InventoryService } from '../../../core/services/inventory.service';
import { OrderReadDto } from '../../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DecimalPipe, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private productSvc   = inject(ProductService);
  private orderSvc     = inject(OrderService);
  private userSvc      = inject(UserService);
  private inventorySvc = inject(InventoryService);

  stats = signal({ products: 0, orders: 0, users: 0, lowStock: 0 });
  recentOrders = signal<OrderReadDto[]>([]);

  ngOnInit(): void {
    this.productSvc.getPaged({ page: 1, size: 1 }).subscribe({
      next: r => this.stats.update(s => ({ ...s, products: r.totalCount })),
      error: () => {}
    });
    this.orderSvc.getAllPaged({ page: 1, size: 5, sortBy: 'date', desc: true }).subscribe({
      next: r => {
        this.stats.update(s => ({ ...s, orders: r.totalCount }));
        this.recentOrders.set(r.items);
      },
      error: () => {}
    });
    this.userSvc.getPaged({ page: 1, size: 1, desc: true }).subscribe({
      next: r => this.stats.update(s => ({ ...s, users: r.totalCount })),
      error: () => {}
    });
    this.inventorySvc.getPaged({ lowStockOnly: true, page: 1, size: 1 }).subscribe({
      next: r => this.stats.update(s => ({ ...s, lowStock: r.totalCount })),
      error: () => {}
    });
  }
}
