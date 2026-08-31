import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AiChatService {

  private apiUrl='http://localhost:8080/api/AiChat/send';
  private http=inject(HttpClient);

  sendMessage(userInput:string):Observable<any>{
    return this.http.post<any>(this.apiUrl,JSON.stringify(userInput),{headers:{'Content-Type':'application/json'}});
  }


}
