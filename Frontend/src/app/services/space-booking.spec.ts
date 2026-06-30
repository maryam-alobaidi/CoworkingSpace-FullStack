import { TestBed } from '@angular/core/testing';

import { SpaceBooking } from './space-booking';

describe('SpaceBooking', () => {
  let service: SpaceBooking;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SpaceBooking);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
