import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { OrderService } from '../../../core/services/order.service';
import { AddressService } from '../../../core/services/address.service';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';
import { AddressReadDto } from '../../../core/models';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css'
})
export class CheckoutComponent implements OnInit {
  readonly cartSvc = inject(CartService);
  private orderSvc = inject(OrderService);
  private addrSvc = inject(AddressService);
  private toast = inject(ToastService);
  private router = inject(Router);

  addresses = signal<AddressReadDto[]>([]);
  selectedAddressId = signal<number | null>(null);
  selectedAddressIdVal: number | null = null;
  placing = signal(false);
  showAddressForm = signal(false);
  savingAddress = signal(false);
  paymentType = 'COD';

  paymentMethods = [
    { value: 'COD',        label: '💵 Cash on Delivery' },
    { value: 'Card',       label: '💳 Credit / Debit Card' },
    { value: 'UPI',        label: '📱 UPI' },
    { value: 'NetBanking', label: '🏦 Net Banking' }
  ];

  newAddr = {
    fullName: '', phone: '', line1: '', line2: '',
    city: '', state: '', postalCode: '', country: 'India'
  };

  ngOnInit(): void {
    this.cartSvc.loadCart().subscribe({ error: () => {} });
    this.addrSvc.getMine().subscribe({
      next: res => {
        this.addresses.set(res.items);
        if (res.items.length > 0) {
          this.selectedAddressId.set(res.items[0].id);
          this.selectedAddressIdVal = res.items[0].id;
        }
      },
      error: () => {}
    });
  }

  get cartEmpty(): boolean {
    return (this.cartSvc.cart()?.items?.length ?? 0) === 0;
  }

  saveAddress(): void {
    if (!this.newAddr.fullName || !this.newAddr.line1 || !this.newAddr.city) {
      this.toast.error('Please fill required address fields.');
      return;
    }
    this.savingAddress.set(true);
    this.addrSvc.create(this.newAddr).subscribe({
      next: addr => {
        this.addresses.update(list => [...list, addr]);
        this.selectedAddressId.set(addr.id);
        this.selectedAddressIdVal = addr.id;
        this.showAddressForm.set(false);
        this.toast.success('Address saved!');
        this.savingAddress.set(false);
        this.newAddr = { fullName: '', phone: '', line1: '', line2: '', city: '', state: '', postalCode: '', country: 'India' };
      },
      error: () => this.savingAddress.set(false)
    });
  }

  placeOrder(): void {
    if (!this.selectedAddressId() || !this.paymentType || this.placing()) return;
    this.placing.set(true);
    this.orderSvc.placeOrder({
      addressId: this.selectedAddressId()!,
      paymentType: this.paymentType,
      notes: ''
    }).subscribe({
      next: res => {
        this.cartSvc.resetCart();
        this.toast.success(`Order #${res.orderNumber} placed successfully!`);
        this.router.navigate(['/orders', res.id]);
      },
      error: () => this.placing.set(false)
    });
  }
}
