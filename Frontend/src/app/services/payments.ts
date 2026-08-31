import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Payments {

  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:8080/api/Payments';

  getTotalRevenue(): Observable<{ totalRevenue: number }> {
    return this.http.get<{ totalRevenue: number }>(`${this.apiUrl}/total-payments`);
  }





}
