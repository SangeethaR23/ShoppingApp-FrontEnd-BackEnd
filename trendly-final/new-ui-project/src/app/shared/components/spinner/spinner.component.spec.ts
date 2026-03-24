import { TestBed } from '@angular/core/testing';
import { SpinnerComponent } from './spinner.component';

describe('SpinnerComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SpinnerComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(SpinnerComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
