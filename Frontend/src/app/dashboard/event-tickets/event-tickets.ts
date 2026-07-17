import { Component, inject, signal } from '@angular/core';
import { TicketService } from '../../services/ticket-service';
import { Auth } from '../../services/auth';
import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-event-tickets',
  imports: [NgClass,    
    DatePipe, 
    RouterLink,CurrencyPipe],
  templateUrl: './event-tickets.html',
  styleUrl: './event-tickets.scss',
})
export class EventTickets {

  private authService = inject(Auth);
  private eventTicketService = inject(TicketService);
  private toastr=inject(ToastrService);

  
  tickets = signal<any[]>([]);
  isLoading = signal<boolean>(true);
  selectedEvent=signal<any|null>(null);
  isModalOpen=false;

  ngOnInit() {
    this.loadUserTickets();
  }

  loadUserTickets() {
    
    const currentUser = this.authService.currentUser();
    const userId = currentUser?.userInfo?.id;

    if (!userId) {
      this.isLoading.set(false);
      return;
    }
    this.isLoading.set(true);
    this.eventTicketService.getUserTickets(userId).subscribe({
      next: (data) => {
        this.tickets.set(data || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching event tickets:', err);
        this.tickets.set([]);
        this.isLoading.set(false);
      }
    });
  }


  payTicket(Id:number){

    this.eventTicketService.rePay(Id).subscribe({
      next: (response: any) => {
     
      const url = response?.sessionUrl || response?.SessionUrl;
      
      if (url) {
        window.location.href = url;
      } else {
        this.toastr.error('Payment URL not found in response');
      }
    },
    error: (err) => {
      this.toastr.error('The payment transfer failed, please try again');
      console.error('Stripe Redirect Error:', err);
    }
    })
  }


  

  openEventReceipt(ticket:any): void {
 
  this.selectedEvent.set(ticket);
}

printEventTicket() {
  // 1. جلب محتوى تذكرة الفعالية فقط عبر الـ ID الخاص بمودل الفعالية
  const printContents = document.getElementById('eventReceiptContent')?.innerHTML;
  
  if (!printContents) {
    this.toastr.error('Event ticket content not found!');
    return;
  }

  const popupWin = window.open('', '_blank', 'top=0,left=0,height=auto,width=auto');
  
  if (popupWin) {
    popupWin.document.open();
    
    popupWin.document.write(`
      <html>
        <head>
          <title>Print Ticket - Vantage Events</title>
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
          <style>
            body { 
              font-family: system-ui, -apple-system, sans-serif; 
              padding: 40px; 
              background-color: #fff !important;
            }
            .bg-light { background-color: #f8f9fa !important; }
            .badge { padding: 6px 12px; border-radius: 4px; display: inline-block; }
            .bg-success-subtle { background-color: #d1e7dd !important; color: #0f5132 !important; border: 1px solid #badbcc; }
            .bg-primary-subtle { background-color: #cfe2ff !important; color: #084298 !important; border: 1px solid #b6d4fe; }
          </style>
        </head>
        <body onload="window.print();window.close()">
          <div class="container" style="max-width: 600px;">
            ${printContents}
          </div>
        </body>
      </html>
    `);
    
    popupWin.document.close();
  } else {
    this.toastr.error('Please allow popups for this website to enable printing.');
  }
}
}
