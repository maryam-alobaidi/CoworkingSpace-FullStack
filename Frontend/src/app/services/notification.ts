import { computed, inject, Injectable, signal } from '@angular/core';
import { NotificationModel } from '../models/notification.model';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class Notification {

  private http=inject(HttpClient);
  private apiUrl='http://localhost:8080/api/Notifications';

    notifications=signal<NotificationModel[]>([]);
    unReadCount=computed(()=>this.notifications().filter(n=>!n.isRead).length);

    loadNotifications(userId:number):void{
      this.http.get<NotificationModel[]>(`${this.apiUrl}/user/${userId}`).subscribe({
        next:(data)=>{

          if(data && data.length>0){
                      const sorted = data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
                      this.notifications.set(sorted);
          }else{
            this.notifications.set([]);//return the array empty if no notification
          }

        },
        error: (err) => {
        console.log('No notifications found or error fetched', err);
        this.notifications.set([]); 
      }
      })
    }


    markAsRead(notificationId:number):void{
          this.http.put(`${this.apiUrl}/mark-read/${notificationId}`, {}, { responseType: 'text' }).subscribe({   
         next:()=>{
         
         this.notifications.update(
          list=>list.map(n=>n.notificationID===notificationId ?{
            ...n,isRead:true
          }:n)
         );
        },
        error: (err) => console.error('Failed to mark as read', err)
      }) 
    }


    clearNotifications():void{
       this.notifications.set([]); 
    }



}
[]