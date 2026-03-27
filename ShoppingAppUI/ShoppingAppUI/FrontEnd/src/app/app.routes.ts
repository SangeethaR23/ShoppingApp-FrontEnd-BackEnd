import { Routes } from '@angular/router';
import { authGuard, noAuthGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/role.guard';

import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { AdminLayoutComponent } from './features/admin/admin-layout/admin-layout.component';
import { AdminDashboardComponent } from './features/admin/admin-dashboard/admin-dashboard.component';
import { AdminProductsComponent } from './features/admin/admin-products/admin-products.component';
import { AdminCategoriesComponent } from './features/admin/admin-categories/admin-categories.component';
import { AdminInventoryComponent } from './features/admin/admin-inventory/admin-inventory.component';
import { AdminOrdersComponent } from './features/admin/admin-orders/admin-orders.component';
import { AdminUsersComponent } from './features/admin/admin-users/admin-users.component';
import { AdminPromosComponent } from './features/admin/admin-promos/admin-promos.component';
import { AdminLogsComponent } from './features/admin/admin-logs/admin-logs.component';
import { HomeComponent } from './features/home/home.component';
import { ProductListComponent } from './features/products/product-list/product-list.component';
import { ProductDetailComponent } from './features/products/product-detail/product-detail.component';
import { CartComponent } from './features/cart/cart.component';
import { CheckoutComponent } from './features/checkout/checkout.component';
import { WishlistComponent } from './features/wishlist/wishlist.component';
import { OrderListComponent } from './features/orders/order-list/order-list.component';
import { OrderDetailComponent } from './features/orders/order-detail/order-detail.component';
import { ProfileComponent } from './features/profile/profile.component';
import { NotFoundComponent } from './features/not-found/not-found.component';

export const routes: Routes = [
  // ── Auth routes ──────────────────────────────────────────────────────────────
  {
    path: 'auth',
    children: [
      { path: 'login',    canActivate: [noAuthGuard], component: LoginComponent },
      { path: 'register', canActivate: [noAuthGuard], component: RegisterComponent },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },

  // ── Admin routes ──────────────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [adminGuard],
    component: AdminLayoutComponent,
    children: [
      { path: '',           component: AdminDashboardComponent },
      { path: 'products',   component: AdminProductsComponent },
      { path: 'categories', component: AdminCategoriesComponent },
      { path: 'inventory',  component: AdminInventoryComponent },
      { path: 'orders',     component: AdminOrdersComponent },
      { path: 'users',      component: AdminUsersComponent },
      { path: 'promos',     component: AdminPromosComponent },
      { path: 'logs',       component: AdminLogsComponent },
    ]
  },

  // ── Public routes ─────────────────────────────────────────────────────────────
  { path: '',             component: HomeComponent },
  { path: 'products',     component: ProductListComponent },
  { path: 'products/:id', component: ProductDetailComponent },

  // ── Protected routes ──────────────────────────────────────────────────────────
  { path: 'cart',       canActivate: [authGuard], component: CartComponent },
  { path: 'checkout',   canActivate: [authGuard], component: CheckoutComponent },
  { path: 'wishlist',   canActivate: [authGuard], component: WishlistComponent },
  { path: 'orders',     canActivate: [authGuard], component: OrderListComponent },
  { path: 'orders/:id', canActivate: [authGuard], component: OrderDetailComponent },
  { path: 'profile',    canActivate: [authGuard], component: ProfileComponent },

  // ── Utility routes ─────────────────────────────────────────────────────────────
  { path: 'forbidden', component: NotFoundComponent },
  { path: '**',        component: NotFoundComponent }
];