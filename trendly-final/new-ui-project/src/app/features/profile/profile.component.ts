import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UserService } from '../../core/services/user.service';
import { AddressService } from '../../core/services/address.service';
import { ToastService } from '../../core/services/toast.service';
import { UserProfileReadDto, AddressReadDto } from '../../core/models';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private userSvc = inject(UserService);
  private addrSvc = inject(AddressService);
  private toast = inject(ToastService);

  profile = signal<UserProfileReadDto | null>(null);
  addresses = signal<AddressReadDto[]>([]);
  activeTab = signal('profile');
  savingProfile = signal(false);
  changingPw = signal(false);
  showAddrModal = signal(false);
  savingAddr = signal(false);
  editingAddr: AddressReadDto | null = null;

  tabs = [
    { id: 'profile',   label: '👤 Profile' },
    { id: 'password',  label: '🔒 Password' },
    { id: 'addresses', label: '📍 Addresses' },
    { id: 'orders',    label: '📦 Orders' }
  ];

  profileForm = { firstName: '', lastName: '', phone: '', dateOfBirth: '' };
  pwForm = { currentPassword: '', newPassword: '' };
  addrForm = {
    label: '', fullName: '', phone: '', line1: '', line2: '',
    city: '', state: '', postalCode: '', country: 'India'
  };

  ngOnInit(): void {
    this.userSvc.getMe().subscribe({
      next: p => {
        this.profile.set(p);
        this.profileForm = {
          firstName: p.firstName,
          lastName: p.lastName,
          phone: p.phone ?? '',
          dateOfBirth: p.dateOfBirth
            ? new Date(p.dateOfBirth).toISOString().split('T')[0]
            : ''
        };
      },
      error: () => {}
    });
    this.addrSvc.getMine().subscribe({
      next: res => this.addresses.set(res.items),
      error: () => {}
    });
  }

  saveProfile(): void {
    this.savingProfile.set(true);
    this.userSvc.updateMe({
      firstName: this.profileForm.firstName,
      lastName: this.profileForm.lastName,
      phone: this.profileForm.phone || undefined,
      dateOfBirth: this.profileForm.dateOfBirth || undefined
    }).subscribe({
      next: p => { this.profile.set(p); this.toast.success('Profile updated!'); this.savingProfile.set(false); },
      error: () => this.savingProfile.set(false)
    });
  }

  changePassword(): void {
    if (!this.pwForm.currentPassword || !this.pwForm.newPassword) return;
    this.changingPw.set(true);
    this.userSvc.changePassword(this.pwForm).subscribe({
      next: () => {
        this.toast.success('Password changed!');
        this.pwForm = { currentPassword: '', newPassword: '' };
        this.changingPw.set(false);
      },
      error: () => this.changingPw.set(false)
    });
  }

  openAddressModal(): void {
    this.editingAddr = null;
    this.addrForm = { label: '', fullName: '', phone: '', line1: '', line2: '', city: '', state: '', postalCode: '', country: 'India' };
    this.showAddrModal.set(true);
  }

  editAddress(addr: AddressReadDto): void {
    this.editingAddr = addr;
    this.addrForm = {
      label: addr.label ?? '', fullName: addr.fullName, phone: addr.phone ?? '',
      line1: addr.line1, line2: addr.line2 ?? '', city: addr.city,
      state: addr.state, postalCode: addr.postalCode, country: addr.country
    };
    this.showAddrModal.set(true);
  }

  closeAddrModal(): void { this.showAddrModal.set(false); }

  saveAddress(): void {
    this.savingAddr.set(true);
    const dto = { ...this.addrForm };
    if (this.editingAddr) {
      this.addrSvc.update(this.editingAddr.id, dto).subscribe({
        next: () => {
          this.addresses.update(list =>
            list.map(a => a.id === this.editingAddr!.id ? { ...a, ...dto } : a)
          );
          this.toast.success('Address updated!');
          this.closeAddrModal();
          this.savingAddr.set(false);
        },
        error: () => this.savingAddr.set(false)
      });
    } else {
      this.addrSvc.create(dto).subscribe({
        next: addr => {
          this.addresses.update(list => [...list, addr]);
          this.toast.success('Address added!');
          this.closeAddrModal();
          this.savingAddr.set(false);
        },
        error: () => this.savingAddr.set(false)
      });
    }
  }

  // Confirm dialog for address delete
  showConfirm = signal(false);
  private pendingDeleteAddrId: number | null = null;

  deleteAddress(id: number): void {
    this.pendingDeleteAddrId = id;
    this.showConfirm.set(true);
  }

  onDeleteAddressConfirmed(): void {
    this.showConfirm.set(false);
    if (this.pendingDeleteAddrId === null) return;
    const id = this.pendingDeleteAddrId;
    this.pendingDeleteAddrId = null;
    this.addrSvc.delete(id).subscribe({
      next: () => {
        this.addresses.update(list => list.filter(a => a.id !== id));
        this.toast.success('Address deleted.');
      },
      error: () => {}
    });
  }

  onDeleteAddressCancelled(): void {
    this.showConfirm.set(false);
    this.pendingDeleteAddrId = null;
  }
}
