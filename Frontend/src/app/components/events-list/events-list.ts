import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { EventSevice } from '../../services/event.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Auth } from '../../services/auth';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-events-list',
  imports: [CommonModule,RouterLink],
  templateUrl: './events-list.html',
  styleUrl: './events-list.scss',
})
export class EventsList implements OnInit{

  eventService=inject(EventSevice);
  authService=inject(Auth);
  toastr=inject(ToastrService);

  public today=new Date();
  loaded=signal(false);

  // Events from the start of today onward, soonest first.
  upcomingEvents=computed(()=>{
    const startOfToday=new Date(); startOfToday.setHours(0,0,0,0);
    return this.eventService.events()
      .filter(ev=>this.getEventDate(ev.eventDate)>=startOfToday)
      .sort((a,b)=>this.getEventDate(a.eventDate).getTime()-this.getEventDate(b.eventDate).getTime());
  });

  ngOnInit(): void {
    this.eventService.getAllEvents().subscribe(
      {
           next: () => this.loaded.set(true),
           error: (err) => { this.loaded.set(true); console.error('Error fetching data:', err); }
      }
    )
  }

  getEventDate(dateString:any):Date{
   return new Date(dateString);
  }


  showMessage(){
   this.toastr.info('Welcome! Please log in to purchase your tickets.','Authentication Required',{
    timeOut:6000,
    progressBar:true,
    closeButton:true
   });

  }
}
