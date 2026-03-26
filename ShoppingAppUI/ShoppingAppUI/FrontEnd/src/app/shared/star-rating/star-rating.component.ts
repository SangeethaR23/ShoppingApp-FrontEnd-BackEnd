import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  template: `
    <div class="stars-row" [style.font-size]="size">
      @for (s of [1,2,3,4,5]; track s) {
        <span
          [style.color]="s <= (hovered || value) ? '#f59e0b' : '#d1d5db'"
          [style.cursor]="readonly ? 'default' : 'pointer'"
          (mouseenter)="!readonly && (hovered = s)"
          (mouseleave)="!readonly && (hovered = 0)"
          (click)="!readonly && select(s)">★</span>
      }
      @if (showCount && count > 0) {
        <span style="font-size:0.8em;color:var(--text-muted);margin-left:4px">({{ count }})</span>
      }
    </div>
  `,
  styles: [`.stars-row { display:inline-flex; align-items:center; gap:2px; }`]
})
export class StarRatingComponent {
  @Input() value = 0;
  @Input() count = 0;
  @Input() readonly = true;
  @Input() size = '1rem';
  @Input() showCount = false;
  @Output() rated = new EventEmitter<number>();
  hovered = 0;

  select(s: number) { this.value = s; this.rated.emit(s); }
}
