import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InventoryService } from '../../../core/services/inventory.service';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryReadDto } from '../../../core/models';

@Component({
  selector: 'app-admin-inventory',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin-inventory.component.html',
  styleUrl: './admin-inventory.component.css'
})
export class AdminInventoryComponent implements OnInit {
  private invSvc = inject(InventoryService);
  private toast  = inject(ToastService);

  inventory       = signal<InventoryReadDto[]>([]);
  loading         = signal(true);
  showAdjustModal = signal(false);
  showSetModal    = signal(false);
  saving          = signal(false);
  selectedInv     = signal<InventoryReadDto | null>(null);
  lowStockOnly    = false;
  skuFilter       = '';
  adjustDelta     = 0;
  adjustReason    = '';
  setQty          = 0;
  private timer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.invSvc.getPaged({
      sku: this.skuFilter || undefined,
      lowStockOnly: this.lowStockOnly || undefined,
      page: 1, size: 100
    }).subscribe({
      next: r => { this.inventory.set(r.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onSearch(): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(() => this.load(), 400);
  }

  openAdjust(inv: InventoryReadDto): void {
    this.selectedInv.set(inv);
    this.adjustDelta = 0;
    this.adjustReason = '';
    this.showAdjustModal.set(true);
  }

  openSet(inv: InventoryReadDto): void {
    this.selectedInv.set(inv);
    this.setQty = inv.quantity;
    this.showSetModal.set(true);
  }

  closeModals(): void {
    this.showAdjustModal.set(false);
    this.showSetModal.set(false);
  }

  doAdjust(): void {
    if (!this.selectedInv()) return;
    this.saving.set(true);
    this.invSvc.adjust(this.selectedInv()!.productId, this.adjustDelta, this.adjustReason || undefined).subscribe({
      next: updated => {
        this.inventory.update(list => list.map(i => i.id === updated.id ? updated : i));
        this.toast.success('Inventory adjusted!');
        this.closeModals();
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  doSet(): void {
    if (!this.selectedInv()) return;
    this.saving.set(true);
    this.invSvc.setQuantity(this.selectedInv()!.productId, this.setQty).subscribe({
      next: updated => {
        this.inventory.update(list => list.map(i => i.id === updated.id ? updated : i));
        this.toast.success('Quantity set!');
        this.closeModals();
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  stockStatus(inv: InventoryReadDto): 'out' | 'low' | 'ok' {
    if (inv.quantity === 0) return 'out';
    if (inv.quantity <= inv.reorderLevel) return 'low';
    return 'ok';
  }
}
