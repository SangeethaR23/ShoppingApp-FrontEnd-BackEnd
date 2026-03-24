import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private authService = inject(AuthService);
  private cart = inject(CartService);
  private toast = inject(ToastService);
  private router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);
  errorMsg = signal('');

  onSubmit(): void {
    if (!this.email || !this.password) return;
    this.loading.set(true);
    this.errorMsg.set('');
    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.toast.success('Welcome back!');
        if (this.authService.userRole() === 'User') {
          this.cart.loadCart().subscribe({ error: () => {} });
        }
        this.router.navigate([this.authService.userRole() === 'Admin' ? '/admin' : '/']);
      },
      error: (err) => {
        this.errorMsg.set(err.error?.message ?? 'Invalid email or password.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }
}
