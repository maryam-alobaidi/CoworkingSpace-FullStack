import { Component, computed, DestroyRef, ElementRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
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
  private host=inject(ElementRef<HTMLElement>);
  private destroyRef=inject(DestroyRef);

  constructor(){
    // On phones/tablets the menu is a Bootstrap collapse; close it after navigating so it
    // doesn't stay open over the new page.
    this.router.events
      .pipe(filter(e=>e instanceof NavigationEnd), takeUntilDestroyed(this.destroyRef))
      .subscribe(()=>this.closeMobileMenu());
  }

  closeMobileMenu(){
    const menu=this.host.nativeElement.querySelector('#navbarNav');
    if(!menu?.classList.contains('show')) return;
    const bootstrap=(window as any).bootstrap;
    if(bootstrap?.Collapse){
      bootstrap.Collapse.getOrCreateInstance(menu,{toggle:false}).hide();
    }else{
      menu.classList.remove('show');
    }
  }

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
