import { TestBed } from '@angular/core/testing';
import { AdminShellComponent } from './admin-shell.component';

describe('AdminShellComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AdminShellComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(AdminShellComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
