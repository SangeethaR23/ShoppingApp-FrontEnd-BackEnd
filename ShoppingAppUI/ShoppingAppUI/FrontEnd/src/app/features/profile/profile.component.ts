import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { UserService } from '../../core/services/user.service';
import { AddressService } from '../../core/services/address.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { UserProfileReadDto } from '../../core/models/user.models';
import { AddressReadDto } from '../../core/models/address.models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, RouterLinkActive, ConfirmDialogComponent],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private userSvc = inject(UserService);
  private addressSvc = inject(AddressService);
  auth = inject(AuthService);
  private toast = inject(ToastService);

  profile = signal<UserProfileReadDto | null>(null);
  addresses = signal<AddressReadDto[]>([]);
  activeTab = signal<'profile' | 'password' | 'addresses'>('profile');
  editingAddress = signal<AddressReadDto | null>(null);
  showAddressForm = signal(false);
  deleteAddressTarget = signal<number | null>(null);
  showDeleteConfirm = signal(false);
  saving = signal(false);

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
    city: ['', Validators.required],
    state: ['', Validators.required],
    postalCode: ['', Validators.required],
    country: ['India', Validators.required]
  });

  ngOnInit() {
    this.userSvc.getMe().subscribe(p => {
      this.profile.set(p);
      this.profileForm.patchValue({
        firstName: p.firstName, lastName: p.lastName,
        phone: p.phone ?? '', dateOfBirth: p.dateOfBirth?.split('T')[0] ?? ''
      });
    });
    this.loadAddresses();
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
      this.addressForm.patchValue(addr as any);
    } else {
      this.addressForm.reset({ country: 'India' });
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
