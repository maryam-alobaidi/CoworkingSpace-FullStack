import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../services/auth';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterModule],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
})
export class AdminLayout implements OnInit{

   authService=inject(Auth);
   toastr=inject(ToastrService);
   adminName=signal<string|null>(null);
   // Mobile drawer state; on desktop the sidebar is always shown via CSS.
   sidebarOpen=signal(false);

   toggleSidebar(){ this.sidebarOpen.update(open => !open); }
   closeSidebar(){ this.sidebarOpen.set(false); }

  ngOnInit() {
  
    const fullName = this.authService.currentUser()?.userInfo?.fullName;
    if (fullName) {
      this.adminName.set(fullName);
    }
  }

   logout(){
    this.authService.logout();
    window.location.href = '/';
   }

  
}
