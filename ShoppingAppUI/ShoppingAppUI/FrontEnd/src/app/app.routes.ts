import { Routes } from '@angular/router';
import { authGuard, noAuthGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // ── Auth routes ─────────────────────────────────────────────────────────────
  // noAuthGuard redirects already-logged-in users away from login/register
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        canActivate: [noAuthGuard],
        loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        canActivate: [noAuthGuard],
        loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },

  // ── Admin routes ─────────────────────────────────────────────────────────────
  // adminGuard = must be logged in AND have Admin role
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      { path: '',           loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'products',   loadComponent: () => import('./features/admin/admin-products/admin-products.component').then(m => m.AdminProductsComponent) },
      { path: 'categories', loadComponent: () => import('./features/admin/admin-categories/admin-categories.component').then(m => m.AdminCategoriesComponent) },
      { path: 'inventory',  loadComponent: () => import('./features/admin/admin-inventory/admin-inventory.component').then(m => m.AdminInventoryComponent) },
      { path: 'orders',     loadComponent: () => import('./features/admin/admin-orders/admin-orders.component').then(m => m.AdminOrdersComponent) },
      { path: 'users',      loadComponent: () => import('./features/admin/admin-users/admin-users.component').then(m => m.AdminUsersComponent) },
      { path: 'promos',     loadComponent: () => import('./features/admin/admin-promos/admin-promos.component').then(m => m.AdminPromosComponent) },
      { path: 'logs',       loadComponent: () => import('./features/admin/admin-logs/admin-logs.component').then(m => m.AdminLogsComponent) },
    ]
  },

  // ── Public routes ─────────────────────────────────────────────────────────────
  { path: '',             loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
  { path: 'products',     loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent) },
  { path: 'products/:id', loadComponent: () => import('./features/products/product-detail/product-detail.component').then(m => m.ProductDetailComponent) },

  // ── Protected routes ──────────────────────────────────────────────────────────
  // authGuard = any authenticated user (User OR Admin role both allowed)
  { path: 'cart',       canActivate: [authGuard], loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent) },
  { path: 'checkout',   canActivate: [authGuard], loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent) },
  { path: 'wishlist',   canActivate: [authGuard], loadComponent: () => import('./features/wishlist/wishlist.component').then(m => m.WishlistComponent) },
  { path: 'orders',     canActivate: [authGuard], loadComponent: () => import('./features/orders/order-list/order-list.component').then(m => m.OrderListComponent) },
  { path: 'orders/:id', canActivate: [authGuard], loadComponent: () => import('./features/orders/order-detail/order-detail.component').then(m => m.OrderDetailComponent) },
  { path: 'profile',    canActivate: [authGuard], loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },

  // ── Utility routes ─────────────────────────────────────────────────────────────
  { path: 'forbidden',  loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent) },
  { path: '**',         loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent) }
];
