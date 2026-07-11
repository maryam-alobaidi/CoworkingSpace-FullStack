import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Payments {

  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7167/api/Payments';

  getTotalRevenue(): Observable<{ totalRevenue: number }> {
    return this.http.get<{ totalRevenue: number }>(`${this.apiUrl}/total-payments`);
  }





}
