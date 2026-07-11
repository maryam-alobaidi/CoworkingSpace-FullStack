import { Component, inject, OnInit, signal } from '@angular/core';
import { SpaceBooking } from '../../services/space-booking';
import { Auth } from '../../services/auth';
import { ToastrService } from 'ngx-toastr';
import { WorkSpace } from '../../models/workspace.model';
import { DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Booking } from '../../components/space-book/booking';



@Component({
  selector: 'app-office-bookings',
  imports: [NgClass, DatePipe,RouterLink],
  templateUrl: './office-bookings.html',
  styleUrl: './office-bookings.scss',
})
export class OfficeBookings implements OnInit {

   private spaceBookingService=inject(SpaceBooking);
   private authService=inject(Auth);
   private toastr=inject(ToastrService);

   booking=signal<any[]>([]);
   isLoading=signal<boolean>(true);
   userId:number|null|undefined=null;
   selectedBooking=signal<any|null>(null);


  ngOnInit(): void {
    this.userId=this.authService.currentUser()?.userInfo.id;
    if(this.userId){
      this.getUserBooking(this.userId);
    }else{
      this.toastr.error("User is not logged in !");
      this.isLoading.set(false);
    }
  }

  getUserBooking(Id: number) {
               return this.spaceBookingService.getUserBookings(Id).subscribe({
                 next: (data) => {

                const today = new Date();

             const mappedBookings = data.map((item: any) => {
             const bookingDate = new Date(item.bookingDate);
             bookingDate.setHours(0, 0, 0, 0);
             today.setHours(0, 0, 0, 0);

             // 🌟 حساب هل انتهت الـ 10 دقائق بناءً على وقت الإنشاء CreatedAt
             const createdTime = new Date(item.createdAt).getTime();
             const currentTime = new Date().getTime();
             const differenceInMinutes = (currentTime - createdTime) / (1000 * 60);

             // إذا كان الحجز معلقاً وتجاوز 10 دقائق، نعتبره ملغياً تلقائياً في الواجهة
             let currentStatus = item.paymentStatus;
             if (item.paymentStatus === 'Pending' && differenceInMinutes > 10) {
               currentStatus = 'Expired'; 
             }
           
             return {
               ...item,
               paymentStatus: currentStatus,
               isUpcoming: bookingDate >= today && currentStatus !== 'Expired'
             };
          });

        this.booking.set(mappedBookings);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error('Failed to get user bookings');
        this.isLoading.set(false);
      }
    });
  }


  goToChackOut(Id: number) {
  
  this.spaceBookingService.rePay(Id).subscribe({
    next: (response: any) => {
      console.log('Response from server:', response); // للتأكد من البيانات في الـ Console
      
      // فحص الحالتين إذا كان الحرف الأول كبير أو صغير منعاً للمشاكل
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
  });
  }

  openReceipt(item:any){
    this.selectedBooking.set(item);
  }


  printReceipt() {
  const printContents = document.getElementById('receiptContent')?.innerHTML;
  
  if (!printContents) {
    this.toastr.error('Receipt content not found!');
    return;
  }

  // 2. فتح نافذة عرض جديدة مؤقتة في المتصفح
  const popupWin = window.open('', '_blank', 'top=0,left=0,height=auto,width=auto');
  
  if (popupWin) {
    popupWin.document.open();
    
    // 3. بناء هيكل المستند المطبوع وحقن تنسيقات Bootstrap الأساسية لتبدو الفاتورة أنيقة
    popupWin.document.write(`
      <html>
        <head>
          <title>Print Receipt - Vantage</title>
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
          <style>
            body { 
              font-family: system-ui, -apple-system, sans-serif; 
              padding: 40px; 
              background-color: #fff !important;
            }
            .bg-light { background-color: #f8f9fa !important; }
            .badge { border: 1px solid #198754; color: #198754; padding: 6px 12px; border-radius: 4px; }
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
