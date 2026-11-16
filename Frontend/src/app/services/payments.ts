import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class Payments {

  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/Payments`;

  getTotalRevenue(): Observable<{ totalRevenue: number }> {
    return this.http.get<{ totalRevenue: number }>(`${this.apiUrl}/total-payments`);
  }





}
