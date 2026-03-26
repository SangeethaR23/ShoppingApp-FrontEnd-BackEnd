import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { CategoryService } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { CategoryReadDto } from '../../../core/models/category.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent, ConfirmDialogComponent, DatePipe],
  templateUrl: './admin-categories.component.html'
})
export class AdminCategoriesComponent implements OnInit {
  private categorySvc = inject(CategoryService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  categories = signal<CategoryReadDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  showForm = signal(false);
  editingCategory = signal<CategoryReadDto | null>(null);
  deleteTarget = signal<number | null>(null);
  showDeleteConfirm = signal(false);

  form = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    parentCategoryId: [null as number | null]
  });

  ngOnInit() { this.load(); }

  load() {
    this.categorySvc.getPaged({ page: this.page(), size: 20, sortBy: 'name', sortDir: 'asc' }).subscribe(r => {
      this.categories.set(r.items);
      this.totalPages.set(Math.ceil(r.totalCount / 20));
    });
  }

  openForm(c?: CategoryReadDto) {
    this.editingCategory.set(c ?? null);
    if (c) this.form.patchValue({ name: c.name, description: c.description ?? '', parentCategoryId: c.parentCategoryId ?? null });
    else this.form.reset();
    this.showForm.set(true);
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const val = this.form.value as any;
    const editing = this.editingCategory();
    if (editing) {
      this.categorySvc.update(editing.id, val).subscribe(() => {
        this.toast.success('Category updated'); this.showForm.set(false); this.load();
      });
    } else {
      this.categorySvc.create(val).subscribe(() => {
        this.toast.success('Category created'); this.showForm.set(false); this.load();
      });
    }
  }

  confirmDelete(id: number) { this.deleteTarget.set(id); this.showDeleteConfirm.set(true); }

  doDelete() {
    const id = this.deleteTarget();
    if (!id) return;
    this.categorySvc.delete(id).subscribe(() => { this.toast.success('Category deleted'); this.load(); });
    this.showDeleteConfirm.set(false);
  }

  onPage(p: number) { this.page.set(p); this.load(); }
  getParentName(id?: number | null) { return id ? this.categories().find(c => c.id === id)?.name ?? '-' : '-'; }
}
