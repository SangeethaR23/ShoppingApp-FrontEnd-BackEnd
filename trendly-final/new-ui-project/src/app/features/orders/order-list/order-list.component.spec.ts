import { TestBed } from '@angular/core/testing';
import { OrderListComponent } from './order-list.component';

describe('OrderListComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [OrderListComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(OrderListComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
