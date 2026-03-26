import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProductReadDto } from '../../../core/models/product.models';
import { CategoryReadDto } from '../../../core/models/category.models';
import { StarRatingComponent } from '../../../shared/star-rating/star-rating.component';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, StarRatingComponent, PaginationComponent, DecimalPipe],
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.css']
})
export class ProductListComponent implements OnInit, OnDestroy {
  private productSvc = inject(ProductService);
  private categorySvc = inject(CategoryService);
  private cartSvc = inject(CartService);
  private wishlistSvc = inject(WishlistService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  products = signal<ProductReadDto[]>([]);
  categories = signal<CategoryReadDto[]>([]);
  totalCount = signal(0);
  page = signal(1);
  pageSize = 12;
  totalPages = signal(1);

  searchCtrl = new FormControl('');
  selectedCategory = signal<number | null>(null);
  sortBy = signal('newest');
  sortDir = signal('desc');
  priceMin = signal<number | null>(null);
  priceMax = signal<number | null>(null);
  inStockOnly = signal(false);

  ngOnInit() {
    this.categorySvc.getPaged({ page: 1, size: 100, sortBy: 'name', sortDir: 'asc' }).subscribe(r => this.categories.set(r.items));

    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['categoryId']) this.selectedCategory.set(+params['categoryId']);
      this.load();
    });

    this.searchCtrl.valueChanges.pipe(
      debounceTime(400), distinctUntilChanged(), takeUntil(this.destroy$)
    ).subscribe(() => { this.page.set(1); this.load(); });
  }

  load() {
    const q = this.searchCtrl.value?.trim();
    if (q) {
      this.productSvc.search({
        nameContains: q,
        categoryId: this.selectedCategory() ?? undefined,
        includeChildren: true,
        sortBy: this.sortBy(),
        sortDir: this.sortDir(),
        priceMin: this.priceMin() ?? undefined,
        priceMax: this.priceMax() ?? undefined,
        inStockOnly: this.inStockOnly(),
        page: this.page(),
        size: this.pageSize
      }).subscribe(r => {
        this.products.set(r.items);
        this.totalCount.set(r.totalCount);
        this.totalPages.set(Math.ceil(r.totalCount / this.pageSize));
      });
    } else {
      this.productSvc.search({
        categoryId: this.selectedCategory() ?? undefined,
        includeChildren: true,
        sortBy: this.sortBy(),
        sortDir: this.sortDir(),
        priceMin: this.priceMin() ?? undefined,
        priceMax: this.priceMax() ?? undefined,
        inStockOnly: this.inStockOnly(),
        page: this.page(),
        size: this.pageSize
      }).subscribe(r => {
        this.products.set(r.items);
        this.totalCount.set(r.totalCount);
        this.totalPages.set(Math.ceil(r.totalCount / this.pageSize));
      });
    }
  }

  onCategoryChange(id: number | null) {
    this.selectedCategory.set(id);
    this.page.set(1);
    this.load();
  }

  onSortChange(val: string) {
    const [by, dir] = val.split('-');
    this.sortBy.set(by);
    this.sortDir.set(dir);
    this.page.set(1);
    this.load();
  }

  onStockToggle() {
    this.inStockOnly.update(v => !v);
    this.page.set(1);
    this.load();
  }

  onPageChange(p: number) { this.page.set(p); this.load(); }

  addToCart(p: ProductReadDto, e: Event) {
    e.stopPropagation();
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/auth/login']); return; }
    this.cartSvc.addItem({ productId: p.id, quantity: 1 }).subscribe(() => this.toast.success('Added to cart'));
  }

  toggleWishlist(p: ProductReadDto, e: Event) {
    e.stopPropagation();
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/auth/login']); return; }
    this.wishlistSvc.toggle({ productId: p.id }).subscribe(r => this.toast.info(r.message));
  }

  isWishlisted(id: number) { return this.wishlistSvc.isInWishlist(id); }
  getImage(p: ProductReadDto) { return p.images?.[0]?.url ?? ''; }

  ngOnDestroy() { this.destroy$.next(); this.destroy$.complete(); }
}
