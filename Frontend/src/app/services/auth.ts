import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { UserModel } from '../models/user.model';
import { Notification } from '../services/notification';
import { UserWithRoleModel } from '../models/user-with-role-model';

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private http = inject(HttpClient);
  private notificationsService=inject(Notification);
  private apiUrl = 'https://localhost:7167/api/Users';

  
  currentUser = signal<{ userInfo: UserModel } | null>(
    localStorage.getItem('user_data') 
      ? { userInfo: JSON.parse(localStorage.getItem('user_data')!) as UserModel } 
      : null
  );

login(loginData: any): Observable<any> {
  const headers = new HttpHeaders({
    'Content-Type': 'application/json'
  });

  return this.http.post(`${this.apiUrl}/Login`, loginData, { headers }).pipe(
    tap((response: any) => {
      const isSuspended = response?.isSuspended;

   
      if (!isSuspended && response && response.token) { 
        const userData: UserModel = {
          id: response.id,
          fullName: response.fullName,
          email: response.email,
          phoneNumber: response.phoneNumber,
          role: response.role,
          isSuspended: response.isSuspended,
        } as UserModel;

        localStorage.setItem('vantage_token', response.token);
        localStorage.setItem('user_data', JSON.stringify(userData)); 
        
        this.currentUser.set({ userInfo: userData });

        if (userData.id) {
          this.notificationsService.loadNotifications(userData.id);
        }
      }
    })
  );
}
  logout() {
  
    localStorage.removeItem('vantage_token');
    localStorage.removeItem('user_data');
    this.currentUser.set(null);
    this.notificationsService.clearNotifications();

  }

  checkAuthStatus() {
    const storedUser = localStorage.getItem('user_data');
    if (storedUser) {
      this.currentUser.set({ userInfo: JSON.parse(storedUser) });
    } else {
      this.currentUser.set(null);
    }
  }

  register(signUpData: any): Observable<any> {
    const headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    return this.http.post(`${this.apiUrl}/Register`, signUpData, { headers });
  }

  getUserInfoById(id: any) {
    return this.http.get(`${this.apiUrl}/${id}`);
  }

  updateUser(id: any, userData: any) {
    return this.http.put(`${this.apiUrl}/${id}`, userData, {
      responseType: 'text' 
    });
  }

  getTotalMembers():Observable<{TotalMembersCount:number}>{
    return this.http.get<{TotalMembersCount:number}>(`${this.apiUrl}/total-members`);
  }

  getAllUsersWithRoles():Observable<UserWithRoleModel[]>{
   return this.http.get<UserWithRoleModel[]>(`${this.apiUrl}/with-role`);
  }

  toggleSuspend(Id: number): Observable<any> {
    return this.http.patch(`${this.apiUrl}/toggle-suspend/${Id}`, {});
  }
  
}