import { TestBed } from '@angular/core/testing';
import { ProductListComponent } from './product-list.component';

describe('ProductListComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ProductListComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(ProductListComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
