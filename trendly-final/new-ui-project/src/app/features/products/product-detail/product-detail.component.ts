import { Component, inject, OnInit, signal, effect } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { CartService } from '../../../core/services/cart.service';
import { ReviewService } from '../../../core/services/review.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { environment } from '../../../../environments/environment';
import { ProductReadDto, ReviewReadDto, PagedResult } from '../../../core/models';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, DatePipe],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css'
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private cartSvc = inject(CartService);
  private reviewSvc = inject(ReviewService);
  readonly auth = inject(AuthService);
  private toast = inject(ToastService);

  readonly productId = signal<number>(0);

  readonly productResource = httpResource<ProductReadDto>(
    () => this.productId() > 0
      ? `${environment.apiUrl}/Products/${this.productId()}`
      : undefined
  );

  readonly reviewsResource = httpResource<PagedResult<ReviewReadDto>>(
    () => this.productId() > 0
      ? `${environment.apiUrl}/Reviews/product/${this.productId()}?page=1&size=20`
      : undefined
  );

  selectedImage = signal('');
  addingToCart = signal(false);
  submittingReview = signal(false);
  qty = 1;
  reviewRating = 5;
  reviewComment = '';

  constructor() {
    effect(() => {
      const p = this.productResource.value();
      if (p?.images?.length && !this.selectedImage()) {
        this.selectedImage.set(p.images[0].url);
      }
    });
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.productId.set(id);
  }

  // FIX: use methods instead of qty++ / qty-- in template (Angular strict template error)
  decQty(): void { if (this.qty > 1) this.qty--; }
  incQty(): void { this.qty++; }

  addToCart(product: ProductReadDto): void {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/login']); return; }
    this.addingToCart.set(true);
    this.cartSvc.addItem({ productId: product.id, quantity: this.qty }).subscribe({
      next: () => { this.toast.success('Added to cart!'); this.addingToCart.set(false); },
      error: () => this.addingToCart.set(false)
    });
  }

  buyNow(product: ProductReadDto): void {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/login']); return; }
    this.cartSvc.addItem({ productId: product.id, quantity: this.qty }).subscribe({
      next: () => this.router.navigate(['/checkout']),
      error: () => {}
    });
  }

  submitReview(productId: number): void {
    this.submittingReview.set(true);
    this.reviewSvc.create({ productId, rating: this.reviewRating, comment: this.reviewComment }).subscribe({
      next: () => {
        this.toast.success('Review submitted!');
        this.reviewComment = '';
        this.reviewRating = 5;
        this.reviewsResource.reload();
        this.submittingReview.set(false);
      },
      error: () => this.submittingReview.set(false)
    });
  }

  stars(avg: number): string {
    const full = Math.round(avg);
    return '★'.repeat(full) + '☆'.repeat(5 - full);
  }
}
