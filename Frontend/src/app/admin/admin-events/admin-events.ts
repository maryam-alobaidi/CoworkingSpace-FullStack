import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { EventSevice } from '../../services/event.service';
import { EventModel } from '../../models/event.model';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-events',
  standalone: true,
  imports: [CommonModule, FormsModule], 
  templateUrl: './admin-events.html',
  styleUrl: './admin-events.scss',
})
export class AdminEvents implements OnInit {
  eventService = inject(EventSevice);

  
  eventList = signal<EventModel[]>([]);
  eventListUpcoming = signal<EventModel[]>([]);
  searchTerm = signal<string>("");
  selectedEvent = signal<EventModel | null>(null);

  ngOnInit(): void {
  
    this.getAllEvent();
    this.getAllUpcomingEvents();
  }

  getAllEvent() {
    this.eventService.getAllEvents().subscribe({
      next: (data) => {
        this.eventList.set(data);
      },
      error: (err) => {
        console.error('Error during getting the events: ', err);
      }
    });
  }

  getAllUpcomingEvents(){
      this.eventService.getAllUpcomingEvents().subscribe({
      next: (data) => {
        this.eventListUpcoming.set(data);
      },
      error: (err) => {
        console.error('Error during getting the events: ', err);
      }
    });
  }

  
  totalEvents = computed(() => this.eventList().length);

  upcomingEvents = computed(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.eventListUpcoming().filter(e => new Date(e.eventDate) >= today).length;
  });

  pastEvents = computed(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.eventList().filter(e => new Date(e.eventDate) < today).length;
  });

  totalRevenue = computed(() => {
    return this.eventList().reduce((sum, e) => {
      const bookedSeats = e.maxAttendees - e.availableSeats;
      return sum + (bookedSeats * e.ticketPrice);
    }, 0);
  });

  filteredEvents = computed(() => {
    const trim = this.searchTerm().toLowerCase().trim();
    return this.eventListUpcoming().filter(e => {
      return !trim || e.title.toLowerCase().includes(trim);
    });
  });

  onSearchChange(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }

  onEditEvent(event: EventModel) {
  this.selectedEvent.set({ ...event });
  }


 updateEvent() {
  const eventToUpdate = this.selectedEvent();
  if (!eventToUpdate) return;

  this.eventService.updateEventService(eventToUpdate.id, eventToUpdate).subscribe({
    next: (response) => {
      this.eventList.set(
        this.eventList().map(e => e.id === eventToUpdate.id ? eventToUpdate : e)
      );

      this.selectedEvent.set(null);
      
      const closeBtn = document.getElementById('closeEditModalBtn');
      closeBtn?.click();
    },
    error: (err) => {
      console.error('Error updating event:', err);
    }
  });
  }

  newEvent = signal<Partial<EventModel>>({
  title: '',
  description: '',
  eventDate: '',
  ticketPrice: 0,
  maxAttendees: 100,
  availableSeats: 100
  });

  createEvent() {
  const eventData = this.newEvent() as EventModel;
  eventData.availableSeats = eventData.maxAttendees;

  this.eventService.addNewEvent(eventData).subscribe({
    next: (response) => {
      this.getAllEvent();

     
      this.newEvent.set({
        title: '',
        description: '',
        eventDate: '',
        ticketPrice: 0,
        maxAttendees: 100,
        availableSeats: 100
      });

     
      const closeBtn = document.getElementById('closeAddModalBtn');
      closeBtn?.click();
    },
    error: (err) => {
      console.error('Error adding new event:', err);
    }
  });
 }

}