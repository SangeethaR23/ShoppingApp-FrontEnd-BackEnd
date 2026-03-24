import { Injectable, signal, computed, linkedSignal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, CurrentUser } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'access_token';
  private readonly baseUrl = `${environment.apiUrl}/Auth`;

  // ─── Angular 21 Signals ────────────────────────────────────────────────
  // currentUser drives everything — isLoggedIn and userRole are computed from it
  readonly currentUser = signal<CurrentUser | null>(this.decodeStoredToken());

  // linkedSignal: stays in sync with currentUser automatically
  readonly isLoggedIn = linkedSignal<CurrentUser | null, boolean>({
    source: this.currentUser,
    computation: (user) => user !== null && this.hasValidToken()
  });

  readonly userRole = computed(() => this.currentUser()?.role ?? '');
  readonly isAdmin  = computed(() => this.userRole() === 'Admin');

  constructor(private http: HttpClient, private router: Router) {}

  login(dto: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, dto).pipe(
      tap(res => this.handleAuth(res)),
      catchError(err => throwError(() => err))
    );
  }

  register(dto: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, dto).pipe(
      tap(res => this.handleAuth(res)),
      catchError(err => throwError(() => err))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private handleAuth(res: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, res.accessToken);
    const user = this.decodeToken(res.accessToken);
    // Setting currentUser automatically updates isLoggedIn via linkedSignal
    this.currentUser.set(user);
  }

  private hasValidToken(): boolean {
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (!token) return false;
    const exp = this.getTokenPayload(token)?.['exp'] as number | undefined;
    if (exp && exp < Math.floor(Date.now() / 1000)) {
      localStorage.removeItem(this.TOKEN_KEY);
      return false;
    }
    return true;
  }

  private decodeStoredToken(): CurrentUser | null {
    const token = localStorage.getItem(this.TOKEN_KEY);
    return token ? this.decodeToken(token) : null;
  }

  private getTokenPayload(token: string): Record<string, unknown> | null {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return null;
    }
  }

  private decodeToken(token: string): CurrentUser | null {
    const p = this.getTokenPayload(token);
    if (!p) return null;
    const id =
      (p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] as string) ??
      (p['nameid'] as string) ??
      (p['sub'] as string);
    const email =
      (p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] as string) ??
      (p['email'] as string) ??
      (p['unique_name'] as string) ?? '';
    const role =
      (p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] as string) ??
      (p['role'] as string) ?? 'User';
    return { id: parseInt(id ?? '0', 10), email, role };
  }
}
