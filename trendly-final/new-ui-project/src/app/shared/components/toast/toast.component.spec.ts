import { TestBed } from '@angular/core/testing';
import { ToastComponent } from './toast.component';

describe('ToastComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ToastComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(ToastComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
