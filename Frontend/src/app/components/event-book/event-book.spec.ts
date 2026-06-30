import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EventBook } from './event-book';

describe('EventBook', () => {
  let component: EventBook;
  let fixture: ComponentFixture<EventBook>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventBook],
    }).compileComponents();

    fixture = TestBed.createComponent(EventBook);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
