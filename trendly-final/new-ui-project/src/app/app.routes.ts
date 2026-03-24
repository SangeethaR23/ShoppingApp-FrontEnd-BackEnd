import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/guards';

import { LoginComponent }    from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';

import { HomeComponent }          from './features/home/home.component';
import { ProductListComponent }   from './features/products/product-list/product-list.component';
import { ProductDetailComponent } from './features/products/product-detail/product-detail.component';

import { CartComponent }        from './features/cart/cart.component';
import { CheckoutComponent }    from './features/orders/checkout/checkout.component';
import { OrderListComponent }   from './features/orders/order-list/order-list.component';
import { OrderDetailComponent } from './features/orders/order-detail/order-detail.component';
import { ProfileComponent }     from './features/profile/profile.component';

import { AdminShellComponent }      from './features/admin/admin-shell/admin-shell.component';
import { DashboardComponent }       from './features/admin/dashboard/dashboard.component';
import { AdminProductsComponent }   from './features/admin/products/admin-products.component';
import { AdminCategoriesComponent } from './features/admin/categories/admin-categories.component';
import { AdminInventoryComponent }  from './features/admin/inventory/admin-inventory.component';
import { AdminOrdersComponent }     from './features/admin/orders/admin-orders.component';
import { AdminUsersComponent }      from './features/admin/users/admin-users.component';

export const routes: Routes = [
  // ─── Public ────────────────────────────────────────────────────────────
  { path: '', component: HomeComponent },
  { path: 'login',    component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  // ─── Products — dynamic routing ───────────────────────────────────────
  // /products                       → all products
  // /products?categoryId=3          → filtered by category
  // /products?search=shirt          → search results
  // /products?categoryId=3&search=x → filtered + searched
  // /products/:id                   → product detail
  { path: 'products',    component: ProductListComponent },
  { path: 'products/:id', component: ProductDetailComponent },

  // ─── Category shortcut routes → redirect to /products?categoryId=:id ─
  // Allows URLs like /category/5 which redirect to /products?categoryId=5
  {
    path: 'category/:id',
    redirectTo: '/products',
    pathMatch: 'full'
  },

  // ─── Protected user routes ─────────────────────────────────────────────
  { path: 'cart',        canActivate: [authGuard], component: CartComponent },
  { path: 'checkout',    canActivate: [authGuard], component: CheckoutComponent },
  { path: 'orders',      canActivate: [authGuard], component: OrderListComponent },
  { path: 'orders/:id',  canActivate: [authGuard], component: OrderDetailComponent },
  { path: 'profile',     canActivate: [authGuard], component: ProfileComponent },

  // ─── Admin panel ───────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [roleGuard],
    component: AdminShellComponent,
    children: [
      { path: '',           redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',  component: DashboardComponent },
      { path: 'products',   component: AdminProductsComponent },
      { path: 'categories', component: AdminCategoriesComponent },
      { path: 'inventory',  component: AdminInventoryComponent },
      { path: 'orders',     component: AdminOrdersComponent },
      { path: 'users',      component: AdminUsersComponent },
    ]
  },

  // ─── Fallback ──────────────────────────────────────────────────────────
  { path: '**', redirectTo: '' }
];
