import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { INDIA_STATES, getDistricts, getCities } from '../../core/data/india-locations.data';
import { UserService } from '../../core/services/user.service';
import { AddressService } from '../../core/services/address.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { WalletService, WalletTransaction } from '../../core/services/wallet.service';
import { UserProfileReadDto } from '../../core/models/user.models';
import { AddressReadDto } from '../../core/models/address.models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, RouterLinkActive, ConfirmDialogComponent, DecimalPipe, DatePipe],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private userSvc = inject(UserService);
  private addressSvc = inject(AddressService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private walletSvc = inject(WalletService);

  profile = signal<UserProfileReadDto | null>(null);
  addresses = signal<AddressReadDto[]>([]);
  activeTab = signal<'profile' | 'password' | 'addresses' | 'wallet'>('profile');
  editingAddress = signal<AddressReadDto | null>(null);
  showAddressForm = signal(false);
  deleteAddressTarget = signal<number | null>(null);
  showDeleteConfirm = signal(false);
  saving = signal(false);

  // Wallet
  walletBalance = signal<number>(0);
  walletTransactions = signal<WalletTransaction[]>([]);
  walletLoading = signal(false);
  creditAmount = signal<number | null>(null);
  crediting = signal(false);

  profileForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: [''],
    dateOfBirth: ['']
  });

  passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  addressForm = this.fb.group({
    label: [''],
    fullName: ['', Validators.required],
    phone: [''],
    line1: ['', Validators.required],
    line2: [''],
    state: ['', Validators.required],
    district: ['', Validators.required],
    city: ['', Validators.required],
    postalCode: ['', Validators.required],
    country: ['India', Validators.required]
  });

  // Location dropdown data
  readonly allStates = INDIA_STATES;
  availableDistricts = signal<string[]>([]);
  availableCities = signal<string[]>([]);

  onStateChange(event: Event) {
    const state = (event.target as HTMLSelectElement).value;
    this.addressForm.patchValue({ district: '', city: '' });
    this.availableDistricts.set(getDistricts(state));
    this.availableCities.set([]);
  }

  onDistrictChange(event: Event) {
    const district = (event.target as HTMLSelectElement).value;
    const state = this.addressForm.get('state')!.value ?? '';
    this.addressForm.patchValue({ city: '' });
    this.availableCities.set(getCities(state, district));
  }

  ngOnInit() {
    this.userSvc.getMe().subscribe(p => {
      this.profile.set(p);
      this.profileForm.patchValue({
        firstName: p.firstName, lastName: p.lastName,
        phone: p.phone ?? '', dateOfBirth: p.dateOfBirth?.split('T')[0] ?? ''
      });
    });
    this.loadAddresses();
    this.loadWallet();
  }

  loadWallet() {
    this.walletLoading.set(true);
    this.walletSvc.getMyWallet().subscribe({
      next: w => {
        this.walletBalance.set(w.balance);
        this.walletSvc.getTransactions(1, 10).subscribe(r => {
          this.walletTransactions.set(r.items);
          this.walletLoading.set(false);
        });
      },
      error: () => this.walletLoading.set(false)
    });
  }

  creditWallet() {
    const amount = this.creditAmount();
    if (!amount || amount <= 0) { this.toast.error('Enter a valid amount'); return; }
    this.crediting.set(true);
    this.walletSvc.credit(amount).subscribe({
      next: r => {
        this.walletBalance.set(r.balance);
        this.toast.success(`₹${amount} credited to wallet`);
        this.creditAmount.set(null);
        this.crediting.set(false);
        this.walletSvc.getTransactions(1, 10).subscribe(res => this.walletTransactions.set(res.items));
      },
      error: () => this.crediting.set(false)
    });
  }

  loadAddresses() {
    this.addressSvc.getMine(1, 20).subscribe(r => this.addresses.set(r.items));
  }

  saveProfile() {
    if (this.profileForm.invalid) { this.profileForm.markAllAsTouched(); return; }
    this.saving.set(true);
    this.userSvc.updateMe(this.profileForm.value as any).subscribe({
      next: p => { this.profile.set(p); this.toast.success('Profile updated'); this.saving.set(false); },
      error: () => this.saving.set(false)
    });
  }

  changePassword() {
    if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; }
    this.saving.set(true);
    const dto = { userId: this.auth.userId()!, ...this.passwordForm.value } as any;
    this.userSvc.changePassword(dto).subscribe({
      next: () => { this.toast.success('Password changed'); this.passwordForm.reset(); this.saving.set(false); },
      error: () => this.saving.set(false)
    });
  }

  openAddressForm(addr?: AddressReadDto) {
    this.editingAddress.set(addr ?? null);
    if (addr) {
      this.availableDistricts.set(getDistricts(addr.state ?? ''));
      this.availableCities.set(getCities(addr.state ?? '', (addr as any).district ?? ''));
      this.addressForm.patchValue(addr as any);
    } else {
      this.addressForm.reset({ country: 'India' });
      this.availableDistricts.set([]);
      this.availableCities.set([]);
    }
    this.showAddressForm.set(true);
  }

  saveAddress() {
    if (this.addressForm.invalid) { this.addressForm.markAllAsTouched(); return; }
    const editing = this.editingAddress();
    if (editing) {
      this.addressSvc.update(editing.id, this.addressForm.value as any).subscribe(() => {
        this.toast.success('Address updated'); this.showAddressForm.set(false); this.loadAddresses();
      });
    } else {
      const dto = { ...this.addressForm.value, userId: this.auth.userId()! } as any;
      this.addressSvc.create(dto).subscribe(() => {
        this.toast.success('Address added'); this.showAddressForm.set(false); this.loadAddresses();
      });
    }
  }

  confirmDeleteAddress(id: number) { this.deleteAddressTarget.set(id); this.showDeleteConfirm.set(true); }

  doDeleteAddress() {
    const id = this.deleteAddressTarget();
    if (!id) return;
    this.addressSvc.delete(id).subscribe(() => { this.toast.success('Address deleted'); this.loadAddresses(); });
    this.showDeleteConfirm.set(false);
  }
}
