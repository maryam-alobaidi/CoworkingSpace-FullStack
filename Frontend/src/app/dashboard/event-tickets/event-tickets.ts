import { Component, inject, signal } from '@angular/core';
import { TicketService } from '../../services/ticket-service';
import { Auth } from '../../services/auth';
import { DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-event-tickets',
  imports: [NgClass,    
    DatePipe, 
    RouterLink],
  templateUrl: './event-tickets.html',
  styleUrl: './event-tickets.scss',
})
export class EventTickets {

  private authService = inject(Auth);
  private eventTicketService = inject(TicketService);

  
  tickets = signal<any[]>([]);
  isLoading = signal<boolean>(true);

  ngOnInit() {
    this.loadUserTickets();
  }

  loadUserTickets() {
    
    const currentUser = this.authService.currentUser();
    const userId = currentUser?.userInfo?.id;

    if (!userId) {
      this.isLoading.set(false);
      return;
    }

   
    this.isLoading.set(true);
    this.eventTicketService.getUserTickets(userId).subscribe({
      next: (data) => {
        this.tickets.set(data || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching event tickets:', err);
        this.tickets.set([]);
        this.isLoading.set(false);
      }
    });
  }
  
}
