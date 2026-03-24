import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);

  email = '';
  password = '';
  firstName = '';
  lastName = '';
  phone = '';
  loading = signal(false);
  errorMsg = signal('');

  onSubmit(): void {
    if (!this.email || !this.password) return;
    this.loading.set(true);
    this.errorMsg.set('');
    this.authService.register({
      email: this.email, password: this.password,
      firstName: this.firstName, lastName: this.lastName, phone: this.phone
    }).subscribe({
      next: () => {
        this.toast.success('Account created! Welcome to ShopZone.');
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.errorMsg.set(err.error?.message ?? 'Registration failed. Please try again.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }
}
