import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private cart = inject(CartService);
  private wishlist = inject(WishlistService);
  private toast = inject(ToastService);
  private router = inject(Router);

  loading = signal(false);
  showPass = signal(false);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: ['']
  });

  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }
  get firstName() { return this.form.get('firstName')!; }
  get lastName() { return this.form.get('lastName')!; }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.auth.register({ ...this.form.value, role: 'User' } as any).subscribe({
      next: () => {
        this.toast.success('Account created! Welcome to ShopZone.');
        this.cart.loadCart().subscribe();
        this.wishlist.load().subscribe();
        this.router.navigate(['/']);
      },
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false)
    });
  }
}
