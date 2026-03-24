import { TestBed } from '@angular/core/testing';
import { ProfileComponent } from './profile.component';

describe('ProfileComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ProfileComponent] }).compileComponents();
  });
  it('should create', () => {
    const fixture = TestBed.createComponent(ProfileComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
