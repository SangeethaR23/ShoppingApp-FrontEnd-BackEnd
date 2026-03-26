import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ProductReadDto } from '../../core/models/product.models';
import { CategoryReadDto } from '../../core/models/category.models';
import { StarRatingComponent } from '../../shared/star-rating/star-rating.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, StarRatingComponent, DecimalPipe],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  private productSvc = inject(ProductService);
  private categorySvc = inject(CategoryService);
  private cartSvc = inject(CartService);
  private wishlistSvc = inject(WishlistService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);

  products = signal<ProductReadDto[]>([]);
  categories = signal<CategoryReadDto[]>([]);
  currentSlide = signal(0);

  slides = [
    { bg: 'linear-gradient(135deg,#6c63ff,#a78bfa)', title: 'Summer Sale', sub: 'Up to 50% off on all products', icon: '🛍️' },
    { bg: 'linear-gradient(135deg,#ff6584,#f97316)', title: 'New Arrivals', sub: 'Discover the latest trends', icon: '✨' },
    { bg: 'linear-gradient(135deg,#43e97b,#38f9d7)', title: 'Free Shipping', sub: 'On orders above ₹499', icon: '🚚' },
  ];

  private slideInterval: any;

  ngOnInit() {
    this.loadData();
    this.startSlideshow();
  }

  loadData() {
    this.productSvc.getPaged({ page: 1, size: 8, sortBy: 'newest', sortDir: 'desc' }).subscribe(r => this.products.set(r.items));
    this.categorySvc.getPaged({ page: 1, size: 20, sortBy: 'name', sortDir: 'asc' }).subscribe(r => this.categories.set(r.items));
  }

  startSlideshow() {
    this.slideInterval = setInterval(() => {
      this.currentSlide.update(s => (s + 1) % this.slides.length);
    }, 4000);
  }

  goSlide(i: number) { this.currentSlide.set(i); }

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

  goProduct(id: number) { this.router.navigate(['/products', id]); }
  goCategory(id: number) { this.router.navigate(['/products'], { queryParams: { categoryId: id } }); }

  getImage(p: ProductReadDto): string {
    return p.images?.[0]?.url ?? '';
  }
}
