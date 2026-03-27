import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { InventoryService } from '../../../core/services/inventory.service';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryReadDto } from '../../../core/models/inventory.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

@Component({
  selector: 'app-admin-inventory',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, PaginationComponent],
  templateUrl: './admin-inventory.component.html'
})
export class AdminInventoryComponent implements OnInit {
  private inventorySvc = inject(InventoryService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  items = signal<InventoryReadDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  showAdjustModal = signal(false);
  adjustTarget = signal<InventoryReadDto | null>(null);
  adjustMode = signal<'adjust' | 'set' | 'reorder'>('adjust');

  filterSku = '';
  filterLowStock = false;
  sortBy = 'name';
  sortDesc = false;

  adjustForm = this.fb.group({ delta: [0], reason: [''], quantity: [0], reorderLevel: [0] });

  ngOnInit() { this.load(); }

  load() {
    this.inventorySvc.getPaged({
      page: this.page(), size: 15,
      sku: this.filterSku || undefined,
      lowStockOnly: this.filterLowStock || undefined,
      sortBy: this.sortBy,
      desc: this.sortDesc
    }).subscribe(r => {
      this.items.set(r.items);
      this.totalPages.set(Math.ceil(r.totalCount / 15));
    });
  }

  applyFilters() { this.page.set(1); this.load(); }

  clearFilters() {
    this.filterSku = '';
    this.filterLowStock = false;
    this.sortBy = 'name';
    this.sortDesc = false;
    this.page.set(1);
    this.load();
  }

  toggleSort(col: string) {
    if (this.sortBy === col) this.sortDesc = !this.sortDesc;
    else { this.sortBy = col; this.sortDesc = false; }
    this.page.set(1);
    this.load();
  }

  sortIcon(col: string) {
    return this.sortBy === col ? (this.sortDesc ? 'v' : '^') : '-';
  }

  openAdjust(item: InventoryReadDto, mode: 'adjust' | 'set' | 'reorder') {
    this.adjustTarget.set(item);
    this.adjustMode.set(mode);
    this.adjustForm.reset({ delta: 0, reason: '', quantity: item.quantity, reorderLevel: item.reorderLevel });
    this.showAdjustModal.set(true);
  }

  saveAdjust() {
    const item = this.adjustTarget()!;
    const val = this.adjustForm.value;
    const mode = this.adjustMode();
    let obs;
    if (mode === 'adjust') {
      obs = this.inventorySvc.adjust(item.productId, { delta: val.delta!, reason: val.reason ?? undefined });
    } else if (mode === 'set') {
      obs = this.inventorySvc.setQuantity(item.productId, { quantity: val.quantity! });
    } else {
      obs = this.inventorySvc.setReorderLevel(item.productId, { reorderLevel: val.reorderLevel! });
    }
    obs.subscribe(() => {
      this.toast.success('Inventory updated');
      this.showAdjustModal.set(false);
      this.load();
    });
  }

  onPage(p: number) { this.page.set(p); this.load(); }
}
