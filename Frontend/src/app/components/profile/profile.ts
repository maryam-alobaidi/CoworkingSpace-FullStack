import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { Auth } from '../../services/auth';
import { UserModel } from '../../models/user.model';
import { RouterLink } from "@angular/router";

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {

  constructor() {
 
    effect(() => {
      const userId = this.authService.currentUser()?.userInfo?.id;
      if (userId) {
        this.getUserInfoById(userId);
      }
    });
  }

  
  public authService=inject(Auth);
  public userInfo=signal<UserModel|null>(null);
  
   userColor: string = '';
   borderColor:string='';

    ngOnInit(): void {
       this.userColor='#27272a';
       this.borderColor = '#1010d6';
    
    }


  getUserInfoById(id:any){
    return this.authService.getUserInfoById(id).subscribe({
      next:(data:any)=>{
          this.userInfo.set(data);
      },
      error: (err) => {
        console.error("Error to get the data", err);
      }
    })

  }






}
