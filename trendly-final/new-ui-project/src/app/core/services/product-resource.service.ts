/**
 * Angular 21: httpResource() — a signal-based alternative to Observable HTTP calls.
 * Use this in components where you need reactive, auto-refetching data tied to signal inputs.
 * For mutations (POST/PUT/DELETE), still use Observable-based ProductService.
 */
import { Injectable, Signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ProductReadDto, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class ProductResourceService {
  private readonly base = `${environment.apiUrl}/Products`;

  /**
   * Creates a reactive httpResource for a product by ID.
   * Automatically re-fetches when the id signal changes.
   */
  productById(id: Signal<number>) {
    return httpResource<ProductReadDto>(() => `${this.base}/${id()}`);
  }
}
