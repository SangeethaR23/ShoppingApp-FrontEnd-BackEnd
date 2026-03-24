import { TestBed } from '@angular/core/testing';
import { AdminProductsComponent } from './admin-products.component';

describe('AdminProductsComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AdminProductsComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(AdminProductsComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
