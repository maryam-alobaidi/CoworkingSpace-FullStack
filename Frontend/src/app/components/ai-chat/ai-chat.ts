import { ChangeDetectorRef, Component, inject, NgZone } from '@angular/core'; 
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiChatService } from '../../services/ai-chat-service';

@Component({
  selector: 'app-ai-chat',
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat.html',
  styleUrl: './ai-chat.scss',
})
export class AiChat {
  private aiChatService = inject(AiChatService);
  private cdr = inject(ChangeDetectorRef);
  private ngZone = inject(NgZone); 
  
  isOpen = false;
  isTyping = false;
  userInput = '';
  messages: { text: string, sender: 'user' | 'ai' | 'system', linkUrl?: string }[] = [
    { text: 'Welcome to Vantage! How can I help you today?', sender: 'ai' }
  ];

  toggleChat() {
    this.isOpen = !this.isOpen;
  }

  sendMessage() {
    if (!this.userInput.trim()) return;

    const text = this.userInput;
    this.messages.push({ text: text, sender: 'user' });
    this.userInput = '';
    this.isTyping = true;
    this.cdr.detectChanges();

    this.aiChatService.sendMessage(text).subscribe({
      next: (res) => {
     this.userInput = '';
        this.ngZone.run(() => {
          this.isTyping = false;
          this.messages.push({ text: res.message, sender: 'ai' });

          if (res.actionType === 'Navigate' && res.targetUrl) {
           this.messages.push({ 
             text: 'Click here to go to the requested page:', 
             sender: 'system',
             linkUrl: res.targetUrl
           });
}
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.isTyping = false;
          this.messages.push({ text: 'Sorry, a connection error occurred.', sender: 'system' });
          this.cdr.detectChanges();
        });
      }
    });
  }
}