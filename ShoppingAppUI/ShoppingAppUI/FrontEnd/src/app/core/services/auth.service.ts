import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, DecodedToken } from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'shopping_jwt';
  private readonly api = `${environment.apiUrl}/auth`;

  private _token = signal<string | null>(this.getStoredToken());

  readonly isLoggedIn = computed(() => !!this._token());
  readonly currentUser = computed(() => this.decodeToken(this._token()));
  readonly userRole = computed(() => this.currentUser()?.role ?? null);
  readonly isAdmin = computed(() => this.userRole() === 'Admin');
  readonly userId = computed(() => this.currentUser()?.id ?? null);
  readonly userName = computed(() => {
    const u = this.currentUser();
    return u ? u.email : null;
  });

  constructor(private http: HttpClient) {}

  login(dto: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/login`, dto).pipe(
      tap(res => this.storeToken(res.accessToken))
    );
  }

  register(dto: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/register`, dto).pipe(
      tap(res => this.storeToken(res.accessToken))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this._token.set(null);
  }

  getToken(): string | null {
    return this._token();
  }

  private storeToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this._token.set(token);
  }

  private getStoredToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private decodeToken(token: string | null): DecodedToken | null {
    if (!token) return null;
    try {
      const payload = token.split('.')[1];
      const decoded = JSON.parse(atob(payload));
      const now = Math.floor(Date.now() / 1000);
      if (decoded.exp && decoded.exp < now) {
        this.logout();
        return null;
      }
      return {
        id: decoded['id'] ?? decoded['nameid'] ?? 0,
        sub: decoded['sub'] ?? decoded['email'] ?? '',
        email: decoded['email'] ?? decoded['sub'] ?? '',
        role: decoded['role'] ?? decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? 'User',
        exp: decoded['exp']
      };
    } catch {
      return null;
    }
  }
}
