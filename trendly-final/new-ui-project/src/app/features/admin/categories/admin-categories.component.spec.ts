import { TestBed } from '@angular/core/testing';
import { AdminCategoriesComponent } from './admin-categories.component';

describe('AdminCategoriesComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AdminCategoriesComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(AdminCategoriesComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
