import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (visible) {
      <div class="confirm-backdrop" (click)="onCancel()">
        <div class="confirm-box" (click)="$event.stopPropagation()">
          <div class="confirm-icon">{{ icon }}</div>
          <h3 class="confirm-title">{{ title }}</h3>
          <p class="confirm-message">{{ message }}</p>
          <div class="confirm-actions">
            <button class="btn btn-cancel" (click)="onCancel()">{{ cancelLabel }}</button>
            <button class="btn btn-confirm-action" [class.btn-danger]="danger" (click)="onConfirm()">{{ confirmLabel }}</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .confirm-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.5);
      display: flex; align-items: center; justify-content: center;
      z-index: 9999; animation: fadeIn 0.15s ease;
    }
    .confirm-box {
      background: #fff; border-radius: 12px; padding: 32px 28px 24px;
      max-width: 400px; width: 90%; text-align: center;
      box-shadow: 0 20px 60px rgba(0,0,0,0.25);
      animation: slideUp 0.2s ease;
    }
    .confirm-icon { font-size: 48px; margin-bottom: 12px; }
    .confirm-title { font-size: 18px; font-weight: 700; margin: 0 0 8px; color: #1a1a1a; }
    .confirm-message { font-size: 14px; color: #666; margin: 0 0 24px; line-height: 1.5; }
    .confirm-actions { display: flex; gap: 12px; justify-content: center; }
    .btn { padding: 10px 24px; border-radius: 6px; border: none; cursor: pointer; font-size: 14px; font-weight: 600; transition: all 0.15s; }
    .btn-cancel { background: #f1f1f1; color: #333; }
    .btn-cancel:hover { background: #e0e0e0; }
    .btn-confirm-action { background: #2563eb; color: #fff; }
    .btn-confirm-action:hover { background: #1d4ed8; }
    .btn-confirm-action.btn-danger { background: #dc2626; }
    .btn-confirm-action.btn-danger:hover { background: #b91c1c; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
  `]
})
export class ConfirmDialogComponent {
  @Input() visible = false;
  @Input() title = 'Are you sure?';
  @Input() message = 'This action cannot be undone.';
  @Input() confirmLabel = 'Confirm';
  @Input() cancelLabel = 'Cancel';
  @Input() danger = true;
  @Input() icon = '⚠️';
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  onConfirm(): void { this.confirmed.emit(); }
  onCancel(): void { this.cancelled.emit(); }
}
