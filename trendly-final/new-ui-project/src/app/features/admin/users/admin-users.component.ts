import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserListItemDto } from '../../../core/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FormsModule, DatePipe, ConfirmDialogComponent],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  private userSvc = inject(UserService);
  private toast   = inject(ToastService);

  users       = signal<UserListItemDto[]>([]);
  loading     = signal(true);
  togglingId  = signal<number | null>(null);
  page        = signal(1);
  totalCount  = signal(0);
  totalPages  = computed(() => Math.ceil(this.totalCount() / 15));
  searchName  = '';
  searchEmail = '';
  searchRole  = '';
  private timer: ReturnType<typeof setTimeout> | null = null;

  // Confirm dialog
  showConfirm    = signal(false);
  confirmTitle   = '';
  confirmMessage = '';
  private pendingUser: UserListItemDto | null = null;
  private pendingRole = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.userSvc.getPaged({
      name:  this.searchName  || undefined,
      email: this.searchEmail || undefined,
      role:  this.searchRole  || undefined,
      sortBy: 'date', desc: true,
      page: this.page(), size: 15
    }).subscribe({
      next: r => {
        this.users.set(r.items);
        this.totalCount.set(r.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(() => { this.page.set(1); this.load(); }, 400);
  }

  goToPage(pg: number): void { this.page.set(pg); this.load(); }

  toggleRole(u: UserListItemDto): void {
    const newRole = u.role === 'Admin' ? 'User' : 'Admin';
    this.pendingUser = u;
    this.pendingRole = newRole;
    this.confirmTitle   = 'Change User Role';
    this.confirmMessage = `Change ${u.email} to "${newRole}"?`;
    this.showConfirm.set(true);
  }

  onRoleConfirmed(): void {
    this.showConfirm.set(false);
    if (!this.pendingUser) return;
    const u = this.pendingUser;
    const newRole = this.pendingRole;
    this.pendingUser = null;
    this.pendingRole = '';
    this.togglingId.set(u.id);
    this.userSvc.updateRole(u.id, newRole).subscribe({
      next: () => {
        this.users.update(list =>
          list.map(x => x.id === u.id ? { ...x, role: newRole } : x)
        );
        this.toast.success(`Role updated to ${newRole}.`);
        this.togglingId.set(null);
      },
      error: () => this.togglingId.set(null)
    });
  }

  onRoleCancelled(): void {
    this.showConfirm.set(false);
    this.pendingUser = null;
    this.pendingRole = '';
  }
}
