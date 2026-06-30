import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class TicketService {
  private http = inject(HttpClient);

  private apiUrl = 'https://localhost:7167/api/EventTickets';

 
  
   creatEventTicket(ticket: any): Observable<any> {
       return this.http.post<any>(`${this.apiUrl}/add`, ticket);
  }

    confirmPayment(paymentData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/confirm-payment`, paymentData);
  }

  getUserTickets(userId:number):Observable<any[]> {
  return this.http.get<any[]>(`${this.apiUrl}/user/${userId}`);
  }


  
}