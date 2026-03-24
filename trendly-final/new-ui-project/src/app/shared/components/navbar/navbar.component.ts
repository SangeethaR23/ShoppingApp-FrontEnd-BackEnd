import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { CategoryService } from '../../../core/services/category.service';
import { CategoryReadDto } from '../../../core/models';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent implements OnInit {
  auth   = inject(AuthService);
  cart   = inject(CartService);
  private router = inject(Router);
  private categorySvc = inject(CategoryService);

  searchQuery = '';
  categories = signal<CategoryReadDto[]>([]);

  // The nav category labels and what to search for in category names
  readonly navCategories = [
    { label: 'MEN',   key: 'men' },
    { label: 'WOMEN', key: 'women' },
    { label: 'KIDS',  key: 'kid' },
    { label: 'HOME',  key: 'home' },
  ];

  ngOnInit(): void {
    this.categorySvc.getPaged({ page: 1, size: 100, sortBy: 'name', sortDir: 'asc' }).subscribe({
      next: res => this.categories.set(res.items),
      error: () => {}
    });
  }

  navigateToCategory(key: string): void {
    const cats = this.categories();
    const match = cats.find(c => c.name.toLowerCase().includes(key.toLowerCase()));
    if (match) {
      this.router.navigate(['/products'], { queryParams: { categoryId: match.id } });
    } else {
      // fallback: search by keyword
      this.router.navigate(['/products'], { queryParams: { search: key } });
    }
  }

  onSearch(event: Event): void {
    const query = (event.target as HTMLInputElement).value.trim();
    this.searchQuery = query;
  }

  submitSearch(event: Event): void {
    event.preventDefault();
    if (this.searchQuery.trim()) {
      this.router.navigate(['/products'], {
        queryParams: { search: this.searchQuery.trim() }
      });
    }
  }
}
