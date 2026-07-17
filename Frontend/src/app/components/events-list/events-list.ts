import { Component, inject, OnInit } from '@angular/core';
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

  ngOnInit(): void {
    this.eventService.getAllEvents().subscribe(
      {
           error: (err) => console.error('Error fetching data:', err)
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
