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
    {
      bg: 'linear-gradient(135deg,#1a1a2e 0%,#6c63ff88 100%)',
      badge: '🔥 Limited Time',
      title: 'Summer Mega Sale',
      sub: 'Up to 50% off on Fashion & more',
      cta: 'Shop Now →',
      tag: '50% OFF',
      img: 'https://images.unsplash.com/photo-1483985988355-763728e1935b?w=700&q=80',
      imgAlt: 'Fashion Sale'
    },
    {
      bg: 'linear-gradient(135deg,#1a1a2e 0%,#f9731688 100%)',
      badge: '🍎 Fresh Picks',
      title: 'Farm Fresh Fruits',
      sub: 'Organic & fresh delivered to your door',
      cta: 'Order Fresh →',
      tag: 'FRESH DAILY',
      img: 'https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=700&q=80',
      imgAlt: 'Fresh Fruits'
    },
    {
      bg: 'linear-gradient(135deg,#1a1a2e 0%,#0ea5e988 100%)',
      badge: '⚡ Tech Deals',
      title: 'Electronics Sale',
      sub: 'Latest gadgets at unbeatable prices',
      cta: 'Explore Deals →',
      tag: 'UP TO 40% OFF',
      img: 'https://images.unsplash.com/photo-1498049794561-7780e7231661?w=700&q=80',
      imgAlt: 'Electronics'
    },
    {
      bg: 'linear-gradient(135deg,#1a1a2e 0%,#10b98188 100%)',
      badge: '💳 Wallet Offer',
      title: 'Pay with Wallet',
      sub: 'Add money & get instant cashback on every order',
      cta: 'Add Money →',
      tag: '10% CASHBACK',
      img: 'https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=700&q=80',
      imgAlt: 'Shopping Offers'
    },
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
  prevSlide() { this.currentSlide.update(s => (s - 1 + this.slides.length) % this.slides.length); }
  nextSlide() { this.currentSlide.update(s => (s + 1) % this.slides.length); }

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

  // Auto-generates a unique gradient from the category name string
  getCategoryGradient(name: string): string {
    const palettes = [
      'linear-gradient(135deg,#6c63ff,#a855f7)',
      'linear-gradient(135deg,#f97316,#ec4899)',
      'linear-gradient(135deg,#0ea5e9,#6c63ff)',
      'linear-gradient(135deg,#10b981,#0ea5e9)',
      'linear-gradient(135deg,#f59e0b,#f97316)',
      'linear-gradient(135deg,#ec4899,#f97316)',
      'linear-gradient(135deg,#14b8a6,#6c63ff)',
      'linear-gradient(135deg,#8b5cf6,#ec4899)',
      'linear-gradient(135deg,#06b6d4,#10b981)',
      'linear-gradient(135deg,#f43f5e,#f97316)',
    ];
    // Pick palette index based on sum of char codes — deterministic per name
    const idx = name.split('').reduce((s, c) => s + c.charCodeAt(0), 0) % palettes.length;
    return palettes[idx];
  }

  getCategoryIcon(name: string): string {
    const n = name.toLowerCase();
    if (n.includes('fruit')) return '🍎';
    if (n.includes('vegetable') || n.includes('veggie')) return '🥦';
    if (n.includes('flower')) return '🌸';
    if (n.includes('electronic') || n.includes('gadget')) return '🖥️';
    if (n.includes('mobile') || n.includes('phone')) return '📱';
    if (n.includes('laptop') || n.includes('computer')) return '💻';
    if (n.includes('fashion') || n.includes('cloth') || n.includes('dress') || n.includes('wear')) return '👗';
    if (n.includes('shoe') || n.includes('footwear')) return '👟';
    if (n.includes('bag') || n.includes('handbag')) return '👜';
    if (n.includes('watch')) return '⌚';
    if (n.includes('jewel') || n.includes('gold') || n.includes('silver')) return '💎';
    if (n.includes('accessor')) return '🕶️';
    if (n.includes('book') || n.includes('stationery')) return '📚';
    if (n.includes('toy') || n.includes('game') || n.includes('kids')) return '🧸';
    if (n.includes('sport') || n.includes('fitness') || n.includes('gym')) return '🏋️';
    if (n.includes('beauty') || n.includes('cosmetic') || n.includes('makeup')) return '💄';
    if (n.includes('health') || n.includes('medicine') || n.includes('pharma')) return '💊';
    if (n.includes('grocery') || n.includes('food')) return '🛒';
    if (n.includes('dairy') || n.includes('milk')) return '🥛';
    if (n.includes('meat') || n.includes('chicken') || n.includes('fish')) return '🍗';
    if (n.includes('bakery') || n.includes('bread') || n.includes('cake')) return '🎂';
    if (n.includes('snack') || n.includes('biscuit') || n.includes('chips')) return '🍿';
    if (n.includes('drink') || n.includes('beverage') || n.includes('juice')) return '🧃';
    if (n.includes('furniture') || n.includes('sofa') || n.includes('chair')) return '🛋️';
    if (n.includes('home') || n.includes('kitchen') || n.includes('decor')) return '🏠';
    if (n.includes('tool') || n.includes('hardware')) return '🔧';
    if (n.includes('pet') || n.includes('animal')) return '🐾';
    if (n.includes('plant') || n.includes('garden')) return '🌿';
    if (n.includes('music') || n.includes('instrument')) return '🎵';
    if (n.includes('camera') || n.includes('photo')) return '📷';
    if (n.includes('tv') || n.includes('television')) return '📺';
    if (n.includes('car') || n.includes('auto') || n.includes('vehicle')) return '🚗';
    if (n.includes('baby') || n.includes('infant')) return '🍼';
    if (n.includes('office') || n.includes('supply')) return '💼';
    return '🏷️';
  }
}
