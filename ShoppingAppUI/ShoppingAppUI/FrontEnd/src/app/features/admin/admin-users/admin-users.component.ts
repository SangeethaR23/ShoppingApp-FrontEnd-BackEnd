import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserListItemDto } from '../../../core/models/user.models';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [PaginationComponent, DatePipe],
  templateUrl: './admin-users.component.html'
})
export class AdminUsersComponent implements OnInit {
  private userSvc = inject(UserService);
  private toast = inject(ToastService);

  users = signal<UserListItemDto[]>([]);
  page = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);
  showRoleModal = signal(false);
  roleTarget = signal<UserListItemDto | null>(null);
  newRole = signal('User');

  ngOnInit() { this.load(); }

  load() {
    this.userSvc.getPaged({ page: this.page(), size: 15 }).subscribe(r => {
      this.users.set(r.items);
      this.totalCount.set(r.totalCount);
      this.totalPages.set(Math.ceil(r.totalCount / 15));
    });
  }

  openRoleModal(user: UserListItemDto) {
    this.roleTarget.set(user);
    this.newRole.set(user.role);
    this.showRoleModal.set(true);
  }

  updateRole() {
    const user = this.roleTarget()!;
    this.userSvc.updateRole(user.id, this.newRole()).subscribe(() => {
      this.toast.success('Role updated');
      this.showRoleModal.set(false);
      this.load();
    });
  }

  onPage(p: number) { this.page.set(p); this.load(); }
}
