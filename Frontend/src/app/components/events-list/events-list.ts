import { Component, inject, OnInit } from '@angular/core';
import { EventSevice } from '../../services/event.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-events-list',
  imports: [CommonModule,RouterLink],
  templateUrl: './events-list.html',
  styleUrl: './events-list.scss',
})
export class EventsList implements OnInit{

  eventService=inject(EventSevice);

  public tody=new Date();

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





}
