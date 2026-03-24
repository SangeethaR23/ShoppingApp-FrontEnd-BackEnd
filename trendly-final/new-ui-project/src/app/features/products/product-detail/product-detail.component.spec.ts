import { TestBed } from '@angular/core/testing';
import { ProductDetailComponent } from './product-detail.component';

describe('ProductDetailComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ProductDetailComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(ProductDetailComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
