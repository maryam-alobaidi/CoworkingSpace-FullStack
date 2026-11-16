import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AiChatService {

  private apiUrl=`${environment.apiBaseUrl}/AiChat/send`;
  private http=inject(HttpClient);

  // One id per browser tab so the assistant remembers earlier messages in the same chat.
  private conversationId = this.loadConversationId();

  sendMessage(userInput:string):Observable<any>{
    return this.http.post<any>(this.apiUrl, { message: userInput, conversationId: this.conversationId });
  }

  private loadConversationId(): string {
    const key = 'vantage_chat_conversation';
    try {
      const existing = sessionStorage.getItem(key);
      if (existing) return existing;
      const id = crypto.randomUUID();
      sessionStorage.setItem(key, id);
      return id;
    } catch {
      return crypto.randomUUID();
    }
  }


}
