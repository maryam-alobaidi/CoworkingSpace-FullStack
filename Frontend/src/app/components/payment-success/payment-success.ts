import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Auth } from '../../services/auth';
import { TicketService } from '../../services/ticket-service';
import { ToastrComponentlessModule, ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-payment-success',
  imports: [RouterModule],
  templateUrl: './payment-success.html',
  styleUrl: './payment-success.scss',
})
export class PaymentSuccess implements OnInit{

  private authService=inject(Auth);
  private route=inject(ActivatedRoute);
  private ticketEventService = inject(TicketService);
  private toastr=inject(ToastrService);

  referenceId:string|null='';
  type:'Space'|'Event'|null=null;
  quantity:string|null=null;
 

  ngOnInit(): void {
     this.authService.checkAuthStatus();
     this.route.queryParamMap.subscribe(params=>{
      if(params.has('bookingId')){
        this.referenceId=params.get('bookingId');
        this.type = 'Space';
      }else if (params.has('ticketId')) {
        this.referenceId = params.get('ticketId');
        this.quantity=params.get('qty');
        this.type = 'Event';

        const stripeSessionId = params.get('session_id') || 'ST-SUCCESS-' + this.referenceId;
        if(this.referenceId){
          const confirmData={
            ticketId:Number(this.referenceId),
            quantity:Number(this.quantity || 1),
            transactionId:stripeSessionId
          };

          this.ticketEventService.confirmPayment(confirmData).subscribe({
            next: (res) => {
              this.toastr.success('Your booking has been confirmed and seats updated!', 'Success');
            },
            error: (err) => {
              console.error('Error during confirmation:', err);
              this.toastr.error('Failed to confirm payment on server.');
            }
        })
      }
    }})
  }
  



}
