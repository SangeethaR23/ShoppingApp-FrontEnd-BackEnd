import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { AddressService } from '../../core/services/address.service';
import { PromoService } from '../../core/services/promo.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { AddressReadDto } from '../../core/models/address.models';
import { PromoReadDto } from '../../core/models/promo.models';
import { PAYMENT_TYPES } from '../../core/models/order.models';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.css']
})
export class CheckoutComponent implements OnInit {
  private fb = inject(FormBuilder);
  private cartSvc = inject(CartService);
  private orderSvc = inject(OrderService);
  private addressSvc = inject(AddressService);
  private promoSvc = inject(PromoService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);

  addresses = signal<AddressReadDto[]>([]);
  selectedAddress = signal<AddressReadDto | null>(null);
  appliedPromo = signal<PromoReadDto | null>(null);
  promoCode = signal('');
  promoError = signal('');
  placing = signal(false);
  showSuccess = signal(false);
  placedOrder = signal<any>(null);
  paymentTypes = PAYMENT_TYPES;

  form = this.fb.group({
    paymentType: ['CashOnDelivery', Validators.required],
    walletUseAmount: [0],
    notes: ['']
  });

  get cart() { return this.cartSvc.cart(); }
  get subtotal() { return this.cartSvc.cartTotal(); }
  get discount() { return this.appliedPromo()?.discountAmount ?? 0; }
  get total() { return Math.max(0, this.subtotal - this.discount); }

  ngOnInit() {
    this.addressSvc.getMine(1, 20).subscribe(r => {
      this.addresses.set(r.items);
      if (r.items.length > 0) this.selectedAddress.set(r.items[0]);
    });
  }

  applyPromo() {
    const code = this.promoCode().trim();
    if (!code) return;
    this.promoSvc.apply({ promoCode: code, cartTotal: this.subtotal }).subscribe({
      next: p => { this.appliedPromo.set(p); this.promoError.set(''); this.toast.success(`Promo applied! -₹${p.discountAmount}`); },
      error: () => { this.promoError.set('Invalid or expired promo code'); this.appliedPromo.set(null); }
    });
  }

  removePromo() { this.appliedPromo.set(null); this.promoCode.set(''); }

  placeOrder() {
    if (!this.selectedAddress()) { this.toast.error('Please select a delivery address'); return; }
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.placing.set(true);
    const dto = {
      userId: this.auth.userId()!,
      addressId: this.selectedAddress()!.id,
      paymentType: this.form.value.paymentType!,
      walletUseAmount: this.form.value.walletUseAmount ?? 0,
      notes: this.form.value.notes ?? undefined,
      promoCode: this.appliedPromo()?.code
    };
    this.orderSvc.placeOrder(dto as any).subscribe({
      next: res => {
        this.placing.set(false);
        this.placedOrder.set(res);
        this.showSuccess.set(true);
        this.cartSvc.clearLocal();
      },
      error: () => this.placing.set(false)
    });
  }

  goOrders() { this.router.navigate(['/orders']); }

  removeFromCart(productId: number) {
    this.cartSvc.removeItem(productId).subscribe();
  }
}
