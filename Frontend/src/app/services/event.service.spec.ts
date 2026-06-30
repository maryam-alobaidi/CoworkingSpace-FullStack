import { TestBed } from '@angular/core/testing';

import { EventSevice } from './event.service';

describe('Event', () => {
  let service: EventSevice;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(EventSevice);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
