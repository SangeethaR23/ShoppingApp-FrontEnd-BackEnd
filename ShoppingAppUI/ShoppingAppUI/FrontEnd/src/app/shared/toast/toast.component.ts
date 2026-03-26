import { Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    <div class="toast-container">
      @for (t of toast.toasts(); track t.id) {
        <div class="toast toast-{{t.type}}" (click)="toast.remove(t.id)">
          <span>{{ icon(t.type) }}</span>
          <span>{{ t.message }}</span>
        </div>
      }
    </div>
  `
})
export class ToastComponent {
  toast = inject(ToastService);

  icon(type: string): string {
    return { success: '✓', error: '✕', info: 'ℹ', warning: '⚠' }[type] ?? 'ℹ';
  }
}
