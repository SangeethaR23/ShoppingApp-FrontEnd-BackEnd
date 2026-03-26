import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { ReviewService } from '../../../core/services/review.service';
import { OrderService } from '../../../core/services/order.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProductReadDto } from '../../../core/models/product.models';
import { ReviewReadDto } from '../../../core/models/review.models';
import { StarRatingComponent } from '../../../shared/star-rating/star-rating.component';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, StarRatingComponent, PaginationComponent, DatePipe, DecimalPipe],
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.css']
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productSvc = inject(ProductService);
  private cartSvc = inject(CartService);
  private wishlistSvc = inject(WishlistService);
  private reviewSvc = inject(ReviewService);
  private orderSvc = inject(OrderService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  product = signal<ProductReadDto | null>(null);
  reviews = signal<ReviewReadDto[]>([]);
  reviewPage = signal(1);
  reviewTotalPages = signal(1);
  selectedImage = signal(0);
  quantity = signal(1);
  hasPurchased = signal(false);
  myReview = signal<ReviewReadDto | null>(null);
  showReviewForm = signal(false);
  addingToCart = signal(false);

  reviewForm = this.fb.group({
    rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment: ['']
  });

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.productSvc.getById(id).subscribe(p => {
      this.product.set(p);
      this.loadReviews();
    });
    if (this.auth.isLoggedIn()) {
      this.checkPurchased(id);
      this.loadMyReview(id);
    }
  }

  loadReviews() {
    const id = this.product()!.id;
    this.productSvc.getReviews(id, this.reviewPage(), 5).subscribe(r => {
      this.reviews.set(r.items);
      this.reviewTotalPages.set(Math.ceil(r.totalCount / 5));
    });
  }

  checkPurchased(productId: number) {
    this.orderSvc.getMyOrders({ page: 1, size: 100 }).subscribe(r => {
      // Check if any delivered order contains this product
      const orderIds = r.items.filter(o => o.status === 'Delivered').map(o => o.id);
      if (orderIds.length > 0) {
        // Check first few orders for the product
        let checked = 0;
        orderIds.slice(0, 5).forEach(oid => {
          this.orderSvc.getById(oid).subscribe(order => {
            if (order.items.some(i => i.productId === productId)) {
              this.hasPurchased.set(true);
            }
            checked++;
          });
        });
      }
    });
  }

  loadMyReview(productId: number) {
    this.reviewSvc.getMineForProduct(productId).subscribe({
      next: r => this.myReview.set(r),
      error: () => {}
    });
  }

  addToCart() {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/auth/login']); return; }
    this.addingToCart.set(true);
    this.cartSvc.addItem({ productId: this.product()!.id, quantity: this.quantity() }).subscribe({
      next: () => { this.toast.success('Added to cart'); this.addingToCart.set(false); },
      error: () => this.addingToCart.set(false)
    });
  }

  buyNow() {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/auth/login']); return; }
    this.cartSvc.addItem({ productId: this.product()!.id, quantity: this.quantity() }).subscribe(() => {
      this.router.navigate(['/cart']);
    });
  }

  toggleWishlist() {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/auth/login']); return; }
    this.wishlistSvc.toggle({ productId: this.product()!.id }).subscribe(r => this.toast.info(r.message));
  }

  isWishlisted() { return this.wishlistSvc.isInWishlist(this.product()?.id ?? 0); }

  submitReview() {
    if (this.reviewForm.invalid) return;
    const pid = this.product()!.id;
    const dto = { userId: this.auth.userId()!, productId: pid, ...this.reviewForm.value } as any;
    if (this.myReview()) {
      this.reviewSvc.update(pid, { rating: dto.rating, comment: dto.comment }).subscribe(() => {
        this.toast.success('Review updated');
        this.showReviewForm.set(false);
        this.loadReviews();
        this.loadMyReview(pid);
      });
    } else {
      this.reviewSvc.create(dto).subscribe(() => {
        this.toast.success('Review submitted');
        this.showReviewForm.set(false);
        this.loadReviews();
        this.loadMyReview(pid);
      });
    }
  }

  onReviewPage(p: number) { this.reviewPage.set(p); this.loadReviews(); }
  changeQty(delta: number) { this.quantity.update(q => Math.max(1, q + delta)); }
}
