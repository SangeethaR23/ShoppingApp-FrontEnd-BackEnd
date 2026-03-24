import { TestBed } from '@angular/core/testing';
import { OrderDetailComponent } from './order-detail.component';

describe('OrderDetailComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [OrderDetailComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(OrderDetailComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
