import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Auth } from '../../services/auth';
import { Notification } from '../../services/notification';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive ,DatePipe],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar  {

 authService = inject(Auth);
 notificationService=inject(Notification);
  router=inject(Router);

 showNotificationDropdown=false;

 
 userColor: string = '';
 borderColor:string='';


  userInitial=computed(()=>{
    const fullName=this.authService.currentUser()?.userInfo.fullName;
    if(!fullName) return '?';
    return fullName.trim().charAt(0).toUpperCase();
   })
 

  ngOnInit(){
   
   this.userColor='#27272a';
   this.borderColor = '#1010d6';

   const currentUserData=this.authService.currentUser();
   if(currentUserData && currentUserData.userInfo.id){
    this.notificationService.loadNotifications(currentUserData.userInfo.id)
   }

  }

  toggleNotifications(){
    this.showNotificationDropdown=!this.showNotificationDropdown;
  }

  handleNotificationClick(notification:any){
    if(!notification.isRead && notification.notificationID){
      this.notificationService.markAsRead(notification.notificationID);
    }
  }


  handleLogout(){
    this.authService.logout();
    window.location.href = '/';
  }




}
