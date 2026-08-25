import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AiChatService {

  private apiUrl='https://localhost:7167/api/AiChat/send';
  private http=inject(HttpClient);

  sendMessage(userInput:string):Observable<any>{
    return this.http.post<any>(this.apiUrl,JSON.stringify(userInput),{headers:{'Content-Type':'application/json'}});
  }


}
