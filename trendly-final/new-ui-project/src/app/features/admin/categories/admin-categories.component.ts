import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { CategoryService } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { CategoryReadDto, CategoryCreateDto, CategoryUpdateDto } from '../../../core/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [FormsModule, DatePipe, ConfirmDialogComponent],
  templateUrl: './admin-categories.component.html',
  styleUrl: './admin-categories.component.css'
})
export class AdminCategoriesComponent implements OnInit {
  private catSvc = inject(CategoryService);
  private toast  = inject(ToastService);

  categories = signal<CategoryReadDto[]>([]);
  loading    = signal(true);
  showModal  = signal(false);
  saving     = signal(false);
  editing: CategoryReadDto | null = null;

  // Confirm dialog state
  showConfirm = signal(false);
  confirmTitle = '';
  confirmMessage = '';
  private pendingDeleteId: number | null = null;

  // FIX: parentCategoryId must be number | undefined (not null) to match CategoryCreateDto
  form: { name: string; description: string; parentCategoryId: number | undefined } = {
    name: '', description: '', parentCategoryId: undefined
  };

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.catSvc.getPaged({ page: 1, size: 100, sortBy: 'name', sortDir: 'asc' }).subscribe({
      next: r => { this.categories.set(r.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  parentName(id?: number | null): string {
    if (!id) return '—';
    return this.categories().find(c => c.id === id)?.name ?? '—';
  }

  openModal(): void {
    this.editing = null;
    this.form = { name: '', description: '', parentCategoryId: undefined };
    this.showModal.set(true);
  }

  editCategory(cat: CategoryReadDto): void {
    this.editing = cat;
    this.form = {
      name: cat.name,
      description: cat.description ?? '',
      parentCategoryId: cat.parentCategoryId ?? undefined
    };
    this.showModal.set(true);
  }

  closeModal(): void { this.showModal.set(false); }

  save(): void {
    this.saving.set(true);
    const dto: CategoryCreateDto = {
      name: this.form.name,
      description: this.form.description || undefined,
      parentCategoryId: this.form.parentCategoryId
    };
    const obs = this.editing
      ? this.catSvc.update(this.editing.id, { ...dto, id: this.editing.id } as CategoryUpdateDto)
      : this.catSvc.create(dto);
    obs.subscribe({
      next: () => {
        this.toast.success(this.editing ? 'Category updated!' : 'Category created!');
        this.closeModal();
        this.load();
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  deleteCategory(id: number): void {
    this.pendingDeleteId = id;
    this.confirmTitle = 'Delete Category';
    this.confirmMessage = 'Are you sure you want to delete this category? Products in it may be affected.';
    this.showConfirm.set(true);
  }

  onDeleteConfirmed(): void {
    this.showConfirm.set(false);
    if (this.pendingDeleteId === null) return;
    const id = this.pendingDeleteId;
    this.pendingDeleteId = null;
    this.catSvc.delete(id).subscribe({
      next: () => {
        this.categories.update(list => list.filter(c => c.id !== id));
        this.toast.success('Category deleted.');
      },
      error: () => {}
    });
  }

  onDeleteCancelled(): void {
    this.showConfirm.set(false);
    this.pendingDeleteId = null;
  }
}
