import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { SpaceBooking } from '../../services/space-booking';

@Component({
  selector: 'app-payment-failed',
  imports: [],
  templateUrl: './payment-failed.html',
  styleUrl: './payment-failed.scss',
})
export class PaymentFailed implements OnInit {

 private route=inject(ActivatedRoute);
 private router=inject(Router);
 private toastr=inject(ToastrService);
 private spaceBookingService=inject(SpaceBooking);

   bookingId: any = this.route.snapshot.queryParamMap.get('bookingId');
 ngOnInit(): void {
  
 
  if(this.bookingId){
    const Id=parseInt(this.bookingId,10);
    this.spaceBookingService.deleteBooking(Id).subscribe({
        next: () => {
          this.toastr.warning('Your temporary reservation has been canceled and released.');
        },
        error: (err) => console.error('Error releasing slot:', err)
      });
    }
  }


  goToBooking(){

   this.router.navigate(['/workspace']);
  }
}