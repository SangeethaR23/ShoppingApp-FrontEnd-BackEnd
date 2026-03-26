import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
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
  imports: [ReactiveFormsModule, PaginationComponent, ConfirmDialogComponent, DatePipe, DecimalPipe],
  templateUrl: './admin-products.component.html',
  styleUrls: ['./admin-products.component.css']
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
  deleteTarget = signal<number | null>(null);
  showDeleteConfirm = signal(false);
  showImageForm = signal(false);
  imageProductId = signal<number | null>(null);
  imageUrl = signal('');

  form = this.fb.group({
    name: ['', Validators.required],
    sku: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    categoryId: [0, [Validators.required, Validators.min(1)]],
    description: [''],
    isActive: [true]
  });

  ngOnInit() {
    this.categorySvc.getPaged({ page: 1, size: 100, sortBy: 'name', sortDir: 'asc' }).subscribe(r => this.categories.set(r.items));
    this.load();
  }

  load() {
    this.productSvc.getPaged({ page: this.page(), size: 15, sortBy: 'newest', sortDir: 'desc' }).subscribe(r => {
      this.products.set(r.items);
      this.totalCount.set(r.totalCount);
      this.totalPages.set(Math.ceil(r.totalCount / 15));
    });
  }

  openForm(p?: ProductReadDto) {
    this.editingProduct.set(p ?? null);
    if (p) {
      this.form.patchValue({ name: p.name, sku: p.sku, price: p.price, categoryId: p.categoryId, isActive: p.isActive });
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

  confirmDelete(id: number) { this.deleteTarget.set(id); this.showDeleteConfirm.set(true); }

  doDelete() {
    const id = this.deleteTarget();
    if (!id) return;
    this.productSvc.delete(id).subscribe(() => { this.toast.success('Product deleted'); this.load(); });
    this.showDeleteConfirm.set(false);
  }

  openImageForm(id: number) { this.imageProductId.set(id); this.imageUrl.set(''); this.showImageForm.set(true); }

  addImage() {
    const url = this.imageUrl().trim();
    if (!url) return;
    this.productSvc.addImage(this.imageProductId()!, { url }).subscribe(() => {
      this.toast.success('Image added'); this.showImageForm.set(false); this.load();
    });
  }

  onPage(p: number) { this.page.set(p); this.load(); }
  getCategoryName(id: number) { return this.categories().find(c => c.id === id)?.name ?? '-'; }
}
