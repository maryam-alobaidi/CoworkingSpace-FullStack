import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RecentEventTicket } from '../models/recent-event-ticket';

@Injectable({
  providedIn: 'root',
})
export class TicketService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:8080/api/EventTickets';

  

  // 1️⃣ إنشاء التذكرة (تأكدي أن الـ ticket يحتوي على الـ Metadata للـ Stripe)
  creatEventTicket(ticket: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/add`, ticket).pipe(
      catchError(this.handleError)
    );
  }


  confirmPayment(paymentData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/confirm-payment`, paymentData);
  }


  getUserTickets(userId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/user/${userId}`).pipe(
      catchError(this.handleError)
    );
  }

 
  rePay(Id: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/repay/${Id}`, {}).pipe(
      catchError(this.handleError)
    );
  }

 
  private handleError(error: HttpErrorResponse) {
    console.error('💥 TicketService Error:', error.message);
    return throwError(() => new Error(error.error?.message || 'حدث خطأ في السيرفر، يرجى المحاولة لاحقاً.'));
  }

   getRecentEventTickets():Observable<RecentEventTicket[]>{
     return this.http.get<RecentEventTicket[]>(`${this.apiUrl}/recent-event-ticket`);
  }
}