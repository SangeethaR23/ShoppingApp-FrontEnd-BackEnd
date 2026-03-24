import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ProductService }  from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { CartService }     from '../../core/services/cart.service';
import { AuthService }     from '../../core/services/auth.service';
import { ToastService }    from '../../core/services/toast.service';
import { ProductReadDto, CategoryReadDto } from '../../core/models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  private productSvc  = inject(ProductService);
  private categorySvc = inject(CategoryService);
  private cartSvc     = inject(CartService);
  private authSvc     = inject(AuthService);
  private toast       = inject(ToastService);
  private router      = inject(Router);

  products   = signal<ProductReadDto[]>([]);
  categories = signal<CategoryReadDto[]>([]);
  loading    = signal(true);

  ngOnInit(): void {
    this.productSvc.getPaged({ page: 1, size: 8, sortBy: 'newest', sortDir: 'desc' }).subscribe({
      next: res => { this.products.set(res.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.categorySvc.getPaged({ page: 1, size: 12, sortBy: 'name', sortDir: 'asc' }).subscribe({
      next: res => this.categories.set(res.items),
      error: () => {}
    });
  }

  addToCart(event: Event, p: ProductReadDto): void {
    event.stopPropagation();
    if (!this.authSvc.isLoggedIn()) { this.router.navigate(['/login']); return; }
    this.cartSvc.addItem({ productId: p.id, quantity: 1 }).subscribe({
      next: () => this.toast.success(`"${p.name}" added to bag!`),
      error: () => {}
    });
  }

  stars(avg: number): string {
    const full = Math.round(avg);
    return '★'.repeat(full) + '☆'.repeat(5 - full);
  }

  catIcon(name: string): string {
    const map: Record<string, string> = {
      electronics: '💻', clothing: '👗', shirts: '👔', shoes: '👟',
      books: '📚', furniture: '🛋️', food: '🍎', sports: '⚽',
      beauty: '💄', toys: '🧸', garden: '🌱', health: '💊',
      jewellery: '💍', automotive: '🚗', music: '🎵', kitchen: '🍳',
      accessories: '👜', women: '👗', men: '👔', kids: '🧒'
    };
    const key = name.toLowerCase();
    for (const k in map) { if (key.includes(k)) return map[k]; }
    return '🛍️';
  }
}
