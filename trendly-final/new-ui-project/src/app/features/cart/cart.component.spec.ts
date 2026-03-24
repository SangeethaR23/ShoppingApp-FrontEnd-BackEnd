import { TestBed } from '@angular/core/testing';
import { CartComponent } from './cart.component';

describe('CartComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [CartComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(CartComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
