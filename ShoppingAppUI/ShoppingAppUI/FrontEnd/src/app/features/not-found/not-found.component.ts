import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="not-found">
      <div class="nf-content">
        <div class="nf-code">404</div>
        <h1>Page Not Found</h1>
        <p>The page you're looking for doesn't exist.</p>
        <a routerLink="/" class="btn btn-primary btn-lg">Go Home</a>
      </div>
    </div>
  `,
  styles: [`
    .not-found { min-height:80vh;display:flex;align-items:center;justify-content:center;text-align:center; }
    .nf-code { font-size:8rem;font-weight:900;color:var(--primary);opacity:0.2;line-height:1; }
    h1 { font-size:2rem;font-weight:800;margin-bottom:0.5rem; }
    p { color:var(--text-muted);margin-bottom:2rem; }
  `]
})
export class NotFoundComponent {}
