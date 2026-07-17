import { Component, inject, signal } from '@angular/core';
import { WorkSpaceService } from '../../services/workspace.service';
import { EventSevice } from '../../services/event.service';
import { DatePipe } from '@angular/common';
import { RouterLink } from "@angular/router";
import { Auth } from '../../services/auth';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-home',
  imports: [DatePipe, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {


   public workSpaceService = inject(WorkSpaceService);
   public eventService=inject(EventSevice); 
   public authService=inject(Auth);
   private toastr=inject(ToastrService);
   workSpaceList=signal<any|null>(null);

   public today = new Date();

  ngOnInit(): void {
    
    this.workSpaceService.getWorkSpace().subscribe({
      next:(data)=>{
        const availableSpaces = data.filter(space => space.isAvailable === true);
        this.workSpaceList.set(availableSpaces)
      },
      error: (err) => console.error('Error fetching data:', err)
      });
     
    this.eventService.getAllEvents().subscribe();
  }

  getEventDate(dateString: any): Date {
    return new Date(dateString);
  }

  showMessage(){
   this.toastr.info('Welcome! Please login or creat a new account to book your space or to get ticket.','Authentication Required',{
    timeOut:6000,
    progressBar:true,
    closeButton:true
   });
   
  }




}
