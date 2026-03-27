import { Component, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProductReadDto } from '../../../core/models/product.models';
import { CategoryReadDto } from '../../../core/models/category.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, DecimalPipe, PaginationComponent, ConfirmDialogComponent],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.css'
})
export class AdminProductsComponent implements OnInit {
  private productSvc = inject(ProductService);
  private categorySvc = inject(CategoryService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  products = signal<ProductReadDto[]>([]);
  categories = signal<CategoryReadDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);

  showForm = signal(false);
  editingProduct = signal<ProductReadDto | null>(null);
  showImageForm = signal(false);
  imageProductId = signal<number | null>(null);
  imageUrl = signal('');
  showDeleteConfirm = signal(false);
  deleteTarget = signal<number | null>(null);

  // Filters & sort
  filterName = '';
  filterCategory = '';
  filterStatus = '';
  sortBy = 'newest';
  sortDesc = true;

  form = this.fb.group({
    name:        ['', Validators.required],
    sku:         ['', Validators.required],
    price:       [0, [Validators.required, Validators.min(0)]],
    categoryId:  [0, Validators.required],
    description: [''],
    isActive:    [true]
  });

  ngOnInit() {
    this.loadCategories();
    this.load();
  }

  loadCategories() {
    this.categorySvc.getPaged({ page: 1, size: 200 }).subscribe(r => this.categories.set(r.items));
  }

  load() {
    const req: any = {
      page: this.page(), size: 15,
      sortBy: this.sortBy,
      sortDir: this.sortDesc ? 'desc' : 'asc'
    };
    if (this.filterName) req.nameContains = this.filterName;
    if (this.filterCategory) req.categoryId = +this.filterCategory;
    if (this.filterStatus) req.isActive = this.filterStatus === 'active';

    this.productSvc.search(req).subscribe(r => {
      this.products.set(r.items);
      this.totalCount.set(r.totalCount);
      this.totalPages.set(Math.ceil(r.totalCount / 15));
    });
  }

  applyFilters() { this.page.set(1); this.load(); }

  clearFilters() {
    this.filterName = ''; this.filterCategory = ''; this.filterStatus = '';
    this.sortBy = 'newest'; this.sortDesc = true;
    this.page.set(1); this.load();
  }

  toggleSort(col: string) {
    if (this.sortBy === col) this.sortDesc = !this.sortDesc;
    else { this.sortBy = col; this.sortDesc = true; }
    this.page.set(1); this.load();
  }

  sortIcon(col: string) { return this.sortBy === col ? (this.sortDesc ? '↓' : '↑') : '↕'; }

  getCategoryName(id: number) {
    return this.categories().find(c => c.id === id)?.name ?? '-';
  }

  openForm(p?: ProductReadDto) {
    this.editingProduct.set(p ?? null);
    if (p) {
      this.form.patchValue({ name: p.name, sku: p.sku, price: p.price, categoryId: p.categoryId, description: p.description ?? '', isActive: p.isActive });
    } else {
      this.form.reset({ isActive: true, price: 0, categoryId: 0 });
    }
    this.showForm.set(true);
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const val = this.form.value as any;
    const editing = this.editingProduct();
    if (editing) {
      this.productSvc.update(editing.id, { ...val, id: editing.id }).subscribe(() => {
        this.toast.success('Product updated'); this.showForm.set(false); this.load();
      });
    } else {
      this.productSvc.create(val).subscribe(() => {
        this.toast.success('Product created'); this.showForm.set(false); this.load();
      });
    }
  }

  toggleActive(p: ProductReadDto) {
    this.productSvc.setActive(p.id, !p.isActive).subscribe(() => {
      this.toast.success(`Product ${p.isActive ? 'deactivated' : 'activated'}`); this.load();
    });
  }

  openImageForm(id: number) {
    this.imageProductId.set(id); this.imageUrl.set(''); this.showImageForm.set(true);
  }

  addImage() {
    const id = this.imageProductId();
    if (!id || !this.imageUrl()) return;
    this.productSvc.addImage(id, { url: this.imageUrl() }).subscribe(() => {
      this.toast.success('Image added'); this.showImageForm.set(false);
    });
  }

  confirmDelete(id: number) { this.deleteTarget.set(id); this.showDeleteConfirm.set(true); }

  doDelete() {
    const id = this.deleteTarget();
    if (!id) return;
    this.productSvc.delete(id).subscribe(() => {
      this.toast.success('Product deleted'); this.load();
    });
    this.showDeleteConfirm.set(false);
  }

  onPage(p: number) { this.page.set(p); this.load(); }
}
