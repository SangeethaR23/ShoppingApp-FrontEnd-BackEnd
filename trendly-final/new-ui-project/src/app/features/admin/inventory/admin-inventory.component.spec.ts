import { TestBed } from '@angular/core/testing';
import { AdminInventoryComponent } from './admin-inventory.component';

describe('AdminInventoryComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AdminInventoryComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(AdminInventoryComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
