import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { SpinnerComponent } from './shared/components/spinner/spinner.component';
import { ToastComponent } from './shared/components/toast/toast.component';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, SpinnerComponent, ToastComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private auth = inject(AuthService);
  private cart = inject(CartService);

  ngOnInit(): void {
    if (this.auth.isLoggedIn() && this.auth.userRole() === 'User') {
      this.cart.loadCart().subscribe({ error: () => {} });
    }
  }
}
