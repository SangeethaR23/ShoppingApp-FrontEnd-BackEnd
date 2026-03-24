# Angular 21 — ShopZone Frontend

## What Changed from Angular 19 → 21

---

### 1. Zoneless Change Detection (BREAKING — biggest change)

**Angular 21 default: no zone.js.**

```ts
// app.config.ts
import { provideZonelessChangeDetection } from '@angular/core';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(), // ← replaces provideZoneChangeDetection()
    ...
  ]
};
```

- `zone.js` removed from `package.json` and `angular.json` polyfills
- Change detection now fires ONLY when signals change or events fire
- Stack traces are clean — no more zone.js noise
- Smaller bundle (~20–30kb less)
- **Best practice**: all state should be in signals (already done in this project)

---

### 2. `linkedSignal` (new in Angular 20, stable in 21)

Used in `AuthService` to derive `isLoggedIn` from `currentUser`:

```ts
// auth.service.ts
readonly currentUser = signal<CurrentUser | null>(this.decodeStoredToken());

// linkedSignal: stays in sync with source signal automatically
readonly isLoggedIn = linkedSignal<CurrentUser | null, boolean>({
  source: this.currentUser,
  computation: (user) => user !== null && this.hasValidToken()
});
```

- `linkedSignal` replaces manually setting two signals in sync
- When `currentUser` changes, `isLoggedIn` updates automatically
- No `effect()` boilerplate needed for derived boolean state

---

### 3. `httpResource()` — Signal-based HTTP (new stable in Angular 21)

Used in `ProductDetailComponent` for reactive data loading:

```ts
// product-detail.component.ts
readonly productId = signal<number>(0);

// Replaces: this.http.get(...).subscribe(...)
// httpResource auto-manages: loading, error, value — all as signals
readonly productResource = httpResource<ProductReadDto>(
  () => this.productId() > 0
    ? `${environment.apiUrl}/Products/${this.productId()}`
    : undefined  // returning undefined pauses the request
);
```

**httpResource API:**
- `.isLoading()` — signal<boolean>
- `.value()` — signal<T | undefined>
- `.error()` — signal<unknown>
- `.reload()` — re-fetches (used after submitting a review)
- When the URL signal changes, auto re-fetches

**Template usage:**
```html
@if (productResource.isLoading()) { <spinner /> }
@else if (productResource.error()) { <error-msg /> }
@else if (productResource.value(); as product) {
  {{ product.name }}
}
```

**Why still use Observables for mutations?**
`httpResource` is read-only GET. POST/PUT/DELETE still use `HttpClient` + Observables.

---

### 4. `effect()` in constructor (Angular 21 — injection context)

```ts
// product-detail.component.ts
constructor() {
  // In Angular 21, effect() must run in injection context (constructor or field initializer)
  effect(() => {
    const p = this.productResource.value();
    if (p?.images?.length && !this.selectedImage()) {
      this.selectedImage.set(p.images[0].url);
    }
  });
}
```

- `effect()` in constructor is the Angular 21 recommended pattern
- No longer needs `runInInjectionContext()` wrapper

---

### 5. Signal Forms (experimental — NOT used in production yet)

Angular 21 introduces Signal Forms as an experimental API. The current project
uses `FormsModule` (template-driven) for compatibility while Signal Forms stabilize.

Signal Forms example (for reference):
```ts
import { form } from '@angular/forms'; // experimental

export class LoginComponent {
  credentials = signal({ email: '', password: '' });
  loginForm = form(this.credentials, (path) => {
    required(path.email);
    required(path.password);
  });
}
```

**Why not used here yet:** Signal Forms are marked experimental in v21 and may have
breaking API changes before stable. This project uses `FormsModule` which is stable,
fully compatible with zoneless, and works perfectly with Angular 21.

---

### 6. `@let` template variable syntax (Angular 21)

```html
<!-- Angular 21: @let for inline template variables -->
@let reviewItems = reviewsResource.value()?.items ?? [];
@if (reviewItems.length === 0) { ... }
@for (r of reviewItems; track r.id) { ... }
```

Used in `product-detail.component.ts` to avoid repeating long expressions.

---

### 7. `withFetch()` is now the default

```ts
// app.config.ts
provideHttpClient(withInterceptors([appInterceptor]), withFetch())
```

In Angular 21, `withFetch()` is the default (uses the browser Fetch API instead of XHR).
The `withFetch` import is kept explicit for clarity.

---

## Version Compatibility

| Package            | Version |
|--------------------|---------|
| @angular/core      | ^21.0.0 |
| @angular/cli       | ^21.0.0 |
| TypeScript         | ~5.7.0  |
| Node.js (min)      | v22.22.0|
| zone.js            | REMOVED |

---

## Running the App

```bash
npm install
ng serve
# App runs at http://localhost:4200
# Backend expected at http://localhost:5000/api
```

## Build for Production

```bash
ng build --configuration production
```
