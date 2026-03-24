import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { CartService } from '../../../core/services/cart.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProductReadDto, CategoryReadDto } from '../../../core/models';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnInit {
  private productSvc  = inject(ProductService);
  private categorySvc = inject(CategoryService);
  private cartSvc     = inject(CartService);
  private authSvc     = inject(AuthService);
  private toast       = inject(ToastService);
  private router      = inject(Router);
  private route       = inject(ActivatedRoute);

  products   = signal<ProductReadDto[]>([]);
  categories = signal<CategoryReadDto[]>([]);
  loading    = signal(true);
  totalCount = signal(0);
  page       = signal(1);
  pageSize   = 20;

  totalPages  = computed(() => Math.ceil(this.totalCount() / this.pageSize));
  pageNumbers = computed(() => {
    const total = this.totalPages(), cur = this.page();
    const pages: number[] = [];
    for (let i = Math.max(1, cur - 2); i <= Math.min(total, cur + 2); i++) pages.push(i);
    return pages;
  });

  searchText         = '';
  selectedCategoryId: number | null = null;
  priceMin: number | null = null;
  priceMax: number | null = null;
  ratingMin: number | null = null;
  sortBy = 'newest';
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    // Dynamic routing: read categoryId and search from URL query params
    this.route.queryParams.subscribe(params => {
      if (params['categoryId']) this.selectedCategoryId = +params['categoryId'];
      if (params['search'])     this.searchText = params['search'];
      this.page.set(1);
      this.loadProducts();
    });

    this.categorySvc.getPaged({ page: 1, size: 100, sortBy: 'name', sortDir: 'asc' }).subscribe({
      next: res => this.categories.set(res.items),
      error: () => {}
    });
  }

  loadProducts(): void {
    this.loading.set(true);
    const [sortField, sortDir] = this.parseSortBy();
    this.productSvc.search({
      categoryId:   this.selectedCategoryId ?? undefined,
      nameContains: this.searchText || undefined,
      priceMin:     this.priceMin ?? undefined,
      priceMax:     this.priceMax ?? undefined,
      ratingMin:    this.ratingMin ?? undefined,
      sortBy:       sortField,
      sortDir,
      page: this.page(),
      size: this.pageSize
    }).subscribe({
      next: res => {
        this.products.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
        // Sync URL with current filters (dynamic routing)
        this.syncUrl();
      },
      error: () => this.loading.set(false)
    });
  }

  /** Keep the URL query params in sync so the page is shareable/bookmarkable */
  private syncUrl(): void {
    const queryParams: Record<string, string | number | null> = {};
    if (this.selectedCategoryId) queryParams['categoryId'] = this.selectedCategoryId;
    if (this.searchText)         queryParams['search']     = this.searchText;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: '',
      replaceUrl: true
    });
  }

  onSearch(): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page.set(1); this.loadProducts(); }, 400);
  }

  onFilter(): void { this.page.set(1); this.loadProducts(); }
  goToPage(pg: number): void { this.page.set(pg); this.loadProducts(); }

  clearFilters(): void {
    this.searchText = '';
    this.selectedCategoryId = null;
    this.priceMin = null;
    this.priceMax = null;
    this.ratingMin = null;
    this.sortBy = 'newest';
    this.page.set(1);
    this.loadProducts();
  }

  private parseSortBy(): [string, string] {
    switch (this.sortBy) {
      case 'price-asc':  return ['price', 'asc'];
      case 'price-desc': return ['price', 'desc'];
      case 'rating':     return ['rating', 'desc'];
      case 'name':       return ['name', 'asc'];
      default:           return ['newest', 'desc'];
    }
  }

  addToCart(p: ProductReadDto): void {
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
}
