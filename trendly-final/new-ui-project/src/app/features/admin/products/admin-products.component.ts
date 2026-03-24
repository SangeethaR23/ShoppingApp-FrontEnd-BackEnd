import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProductReadDto, CategoryReadDto, ProductCreateDto, ProductUpdateDto } from '../../../core/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [FormsModule, DecimalPipe, ConfirmDialogComponent],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.css'
})
export class AdminProductsComponent implements OnInit {
  private productSvc  = inject(ProductService);
  private categorySvc = inject(CategoryService);
  private toast       = inject(ToastService);

  products   = signal<ProductReadDto[]>([]);
  categories = signal<CategoryReadDto[]>([]);
  loading    = signal(true);
  showModal  = signal(false);
  saving     = signal(false);
  editingProduct: ProductReadDto | null = null;
  page       = signal(1);
  totalCount = signal(0);
  totalPages = signal(0);
  searchText = '';
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  // Confirm dialog state
  showConfirm = signal(false);
  confirmTitle = '';
  confirmMessage = '';
  private pendingDeleteId: number | null = null;

  form: ProductCreateDto & { id?: number } = {
    name: '', sku: '', price: 0, categoryId: 0, description: '', isActive: true
  };

  ngOnInit(): void {
    this.load();
    this.categorySvc.getPaged({ page: 1, size: 100, sortBy: 'name', sortDir: 'asc' }).subscribe({
      next: r => this.categories.set(r.items),
      error: () => {}
    });
  }

  load(): void {
    this.loading.set(true);
    this.productSvc.search({
      nameContains: this.searchText || undefined,
      page: this.page(), size: 20,
      sortBy: 'newest', sortDir: 'desc'
    }).subscribe({
      next: r => {
        this.products.set(r.items);
        this.totalCount.set(r.totalCount);
        this.totalPages.set(Math.ceil(r.totalCount / 20));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page.set(1); this.load(); }, 400);
  }

  goToPage(pg: number): void { this.page.set(pg); this.load(); }

  catName(id: number): string {
    return this.categories().find(c => c.id === id)?.name ?? '—';
  }

  openModal(): void {
    this.editingProduct = null;
    this.form = { name: '', sku: '', price: 0, categoryId: 0, description: '', isActive: true };
    this.showModal.set(true);
  }

  editProduct(p: ProductReadDto): void {
    this.editingProduct = p;
    this.form = {
      id: p.id, name: p.name, sku: p.sku, price: p.price,
      categoryId: p.categoryId, description: (p as any).description ?? '', isActive: p.isActive
    };
    this.showModal.set(true);
  }

  closeModal(): void { this.showModal.set(false); }

  saveProduct(): void {
    this.saving.set(true);
    const obs = this.editingProduct
      ? this.productSvc.update(this.editingProduct.id, this.form as ProductUpdateDto)
      : this.productSvc.create(this.form);
    obs.subscribe({
      next: () => {
        this.toast.success(this.editingProduct ? 'Product updated!' : 'Product created!');
        this.closeModal();
        this.load();
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  toggleActive(p: ProductReadDto): void {
    this.productSvc.setActive(p.id, !p.isActive).subscribe({
      next: () => {
        this.products.update(list =>
          list.map(x => x.id === p.id ? { ...x, isActive: !x.isActive } : x)
        );
        this.toast.info(`Product ${p.isActive ? 'deactivated' : 'activated'}.`);
      },
      error: () => {}
    });
  }

  deleteProduct(id: number): void {
    this.pendingDeleteId = id;
    this.confirmTitle = 'Delete Product';
    this.confirmMessage = 'Are you sure you want to delete this product? This cannot be undone.';
    this.showConfirm.set(true);
  }

  onDeleteConfirmed(): void {
    this.showConfirm.set(false);
    if (this.pendingDeleteId === null) return;
    const id = this.pendingDeleteId;
    this.pendingDeleteId = null;
    this.productSvc.delete(id).subscribe({
      next: () => {
        this.products.update(list => list.filter(p => p.id !== id));
        this.toast.success('Product deleted.');
      },
      error: () => {}
    });
  }

  onDeleteCancelled(): void {
    this.showConfirm.set(false);
    this.pendingDeleteId = null;
  }
}
