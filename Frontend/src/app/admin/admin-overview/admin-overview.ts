import { Component, inject, OnInit, signal } from '@angular/core';
import { Payments } from '../../services/payments';
import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { SpaceBooking } from '../../services/space-booking';
import { EventSevice } from '../../services/event.service';
import { Auth } from '../../services/auth';
import { RecentSpaceReservation } from '../../models/recent-space-reservation';
import { TicketService } from '../../services/ticket-service';
import { RecentEventTicket } from '../../models/recent-event-ticket';
import { RouterLink } from '@angular/router';


@Component({
  selector: 'app-admin-overview',
  imports: [CurrencyPipe,NgClass,RouterLink,DatePipe],
  templateUrl: './admin-overview.html',
  styleUrl: './admin-overview.scss',
})
export class AdminOverview implements OnInit{


  private paymentsService=inject(Payments);
  private spaceBookingService=inject(SpaceBooking);
  private eventService=inject(EventSevice);
  private authService=inject(Auth);
  private eventTicketService=inject(TicketService);


  totalRevenue=signal<number|null>(null);
  activeBookingsCount=signal<number|null>(null);
  upcomingEventsCount=signal<number|null>(null);
  totalMembersCount=signal<number|null>(null);
  recentBookReservations = signal<RecentSpaceReservation[]>([]);
  recentEventTickets = signal<RecentEventTicket[]>([]);

  ngOnInit(): void {
    this.GetTotalRevenue();
    this.getActiveBookingsCount();
    this.getUpcomingEvents();
    this.getTotalMembers();
    this.getRecentReservations();
    this.getRecentEventTickets();
  }


  GetTotalRevenue(){
   return this.paymentsService.getTotalRevenue().subscribe({
    next:(data:any)=>{
      const revenue = data?.totalRevenue;
        this.totalRevenue.set(revenue);
    },
    error:(er)=>{
      console.error('Error to get the total revenue',er);
    }
   })
  }

   getUpcomingEvents(){
     return this.eventService.getUpcomingEvents().subscribe({
    next:(data:any)=>{
      const count = data?.countUpcomingEvents;
        this.upcomingEventsCount.set(count);
    },
    error:(er)=>{
      console.error('Error to get the Upcoming Events Count!',er);
    }
   })
  }

  getActiveBookingsCount(){
    return this.spaceBookingService.getActiveBookingsCount().subscribe({
    next:(data:any)=>{
      const count = data?.countActiveBookings ;
        this.activeBookingsCount.set(count);
    },
    error:(er)=>{
      console.error('Error to get the active booking count!',er);
    }
   })
  }

  getTotalMembers(){
    return this.authService.getTotalMembers().subscribe({
    next:(data:any)=>{
      const count = data?.countTotalMembers ;
        this.totalMembersCount.set(count);
    },
    error:(er)=>{
      console.error('Error to get the total members count!',er);
    }
   })
  }

  getRecentReservations(): void {
  this.spaceBookingService.getRecentReservations().subscribe({
    next: (data: RecentSpaceReservation[]) => {
      this.recentBookReservations.set(data);
    },
    error: (er) => {
      console.error('Error getting recent reservations!', er);
    }
  });
  }

  getRecentEventTickets(): void{
    this.eventTicketService.getRecentEventTickets().subscribe({
      next:(data:RecentEventTicket[])=>{
        this.recentEventTickets.set(data);
      },
      error: (er) => {
      console.error('Error getting recent Event!', er);
    }
    })
  }



}
