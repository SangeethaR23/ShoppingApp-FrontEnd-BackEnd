import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PromoService } from '../../../core/services/promo.service';
import { ToastService } from '../../../core/services/toast.service';
import { PromoReadDto } from '../../../core/models/promo.models';

@Component({
  selector: 'app-admin-promos',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './admin-promos.component.html'
})
export class AdminPromosComponent implements OnInit {
  private promoSvc = inject(PromoService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  promos = signal<PromoReadDto[]>([]);
  showForm = signal(false);

  form = this.fb.group({
    code: ['', Validators.required],
    discountAmount: [0, [Validators.required, Validators.min(1)]],
    startDateUtc: ['', Validators.required],
    endDateUtc: ['', Validators.required],
    minOrderAmount: [null as number | null]
  });

  ngOnInit() { this.load(); }

  load() { this.promoSvc.getAll().subscribe(p => this.promos.set(p)); }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const val = this.form.value as any;
    this.promoSvc.create(val).subscribe(() => {
      this.toast.success('Promo created'); this.showForm.set(false); this.load();
    });
  }

  toggleActive(p: PromoReadDto) {
    this.promoSvc.activate(p.id, !p.isActive).subscribe(() => {
      this.toast.success(`Promo ${p.isActive ? 'deactivated' : 'activated'}`); this.load();
    });
  }
}
