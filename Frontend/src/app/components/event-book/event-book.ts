import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { EventModel } from '../../models/event.model';
import { EventSevice } from '../../services/event.service';
import { ToastrService } from 'ngx-toastr';
import { Auth } from '../../services/auth';

import { TicketService } from '../../services/ticket-service';

@Component({
  selector: 'app-event-book',
  standalone: true, 
  imports: [CommonModule], 
  templateUrl: './event-book.html',
  styleUrl: './event-book.scss'
})
export class EventBook implements OnInit {

 private router=inject(Router);
 private  route=inject(ActivatedRoute);
 private eventService=inject(EventSevice);
 private toastr = inject(ToastrService);
 private authService=inject(Auth);
 private ticketEventService=inject(TicketService);


 eventDetails=signal<EventModel|null>(null);
 quantity=signal<number>(1);
 ticketId=signal<number|null>(null);


  ngOnInit(): void {
    const eventId=this.route.snapshot.paramMap.get('id');
    if(eventId){
      this.loadEventDetails(eventId);
    }
  }

  loadEventDetails(id:string){
    return this.eventService.getEventDetailsById(id).subscribe({
      next:(data)=>{
        this.eventDetails.set(data);
      },
      error:(err)=>{
      this.toastr.error('Failed to load event details. Please try again.');
      }
    })
  }


  decreaseQty(){
     this.quantity.update(q=>q-1);
  }
  
  increaseQty(){
     this.quantity.update(q=>q+1);
  }


  checkoutTickets() {
  const details = this.eventDetails();
  if (!details) {
    return;
  }

  const currentUserId = this.authService.currentUser()?.userInfo.id || 0;
  
  const ticketDto = {
    eventId: details.id,
    userId: currentUserId,
    quantity: this.quantity()
  };
  
  this.ticketEventService.creatEventTicket(ticketDto).subscribe({
    next: (res: any) => {
      const url = res.sessionUrl || res.SessionUrl;

    if (url) {
      this.toastr.info('Redirecting to secure payment gate...');
      window.location.href=url;
      }else {
     
        const ticketIds=res.ticketIds.join(',');
        this.router.navigate([`/payment-success`],{
          queryParams:{
            ticketIds:ticketIds,
            referenceType:'Event',
            Qty:res.ticketIds.length
          }
        });
    }
    },
    error: (err) => {
      this.toastr.error('An error occurred while initiating checkout.');
    }
  });
  }

}