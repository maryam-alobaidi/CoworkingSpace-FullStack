import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OfficeBookings } from './office-bookings';

describe('OfficeBookings', () => {
  let component: OfficeBookings;
  let fixture: ComponentFixture<OfficeBookings>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OfficeBookings],
    }).compileComponents();

    fixture = TestBed.createComponent(OfficeBookings);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
