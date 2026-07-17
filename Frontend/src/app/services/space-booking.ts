import { formatDate } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { WorkSpace } from '../models/workspace.model';

@Injectable({
  providedIn: 'root',
})
export class SpaceBooking {

  private http=inject(HttpClient);
  private apiUrl = 'https://localhost:7167/api/SpaceBookings';

  getBookedSlots(spaceId:number,bookingDate:any):Observable<string[]>{

     const formattedDate = formatDate(bookingDate, 'yyyy-MM-dd', 'en-US');
     
    const params=new HttpParams()
    .set('spaceId',spaceId.toString())
    .set('bookingDate',formattedDate);

    return this.http.get<string[]>(`${this.apiUrl}/GetBookedSlots`,{params})
  }

  
  creatBooking(bookingData:any):Observable<any>{

    return this.http.post<any>(`${this.apiUrl}/add`,bookingData);
  }


  deleteBooking(Id:number){
    return this.http.delete(`${this.apiUrl}/delete/${Id}`);
  }


  getUserBookings(Id:number):Observable<WorkSpace[]>{
    return this.http.get<WorkSpace[]>(`${this.apiUrl}/user/${Id}`);
  }

  rePay(Id:number){
    return this.http.post(`${this.apiUrl}/repay/${Id}`, {});
  }

   getActiveBookingsCount(): Observable<{ countActiveBookings: number }> {
  
    return this.http.get<{ countActiveBookings: number }>(`${this.apiUrl}/active-bookings`);
  }

  getRecentReservations():Observable<any[]>{
     return this.http.get<any[]>(`${this.apiUrl}/recent-reservation`);
  }

 

}
