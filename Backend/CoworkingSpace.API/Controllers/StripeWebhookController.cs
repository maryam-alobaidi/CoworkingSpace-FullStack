using CoworkingSpace.BLL;
using CoworkingSpace.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.V2;
using System.Runtime.Intrinsics.Arm;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/webhook")]
    public class StripeWebhookController : Controller
    {
        // هذا السر ستحصلين عليه عند تشغيل Stripe CLI (سأشرحه لكِ لاحقاً)
        const string endpointSecret = "whsec_e67b06dbc1d89e5c5cd52f802fa16d914289cb6cdf3bdd4f5d6f02f0878be101";

        private readonly IEmailService _emailService;

        public StripeWebhookController(IEmailService emailService)
        {
            _emailService = emailService;
        }



       
        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                //عندما تنجح عملية الدفع، يرسل Stripe رسالة (Webhook). هذا السطر وظيفته التأكد من أن الرسالة حقيقية وقادمة من Stripe فعلاً، وليس "هكر" يحاول خداع النظام وإيهامه بأنه دفع وهو لم يدفع.

                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], endpointSecret);

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

               
                    string type = paymentIntent.Metadata.ContainsKey("Type") ? paymentIntent.Metadata["Type"] : "";

                 
                    int referenceId = 0;
                    if (type == "Space")
                    {
                    
                        referenceId = int.Parse(paymentIntent.Metadata["BookingId"]);
                    }
                    else if (type == "Event")
                    {
                        referenceId = int.Parse(paymentIntent.Metadata["TicketId"]);
                    }

                    if (referenceId > 0)
                    {
                        await _HandleSuccessfulPayment(referenceId, type, paymentIntent);
                    }
                }

                return Ok();
            }
            catch (Exception ex) 
            {
             
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");

                return BadRequest();
            }
        }
        private async Task _HandleSuccessfulPayment(int referenceId, string type, PaymentIntent intent)
        {
            bool isSaved = false;

            if (type == "Event")
            {
                // معالجة التذاكر (كودك القديم)
                var ticket = await clsEventTickets.Find(referenceId);
                if (ticket != null)
                {
                    ticket.PaymentStatus = "Completed";
                    ticket.TransactionId = intent.Id;
                    var user = clsUsers.Find(ticket.UserId);
                    if (user != null) await ticket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                    isSaved = true;
                }
            }
         
            if (type == "Space")
            {
                var booking = clsSpaceBookings.Find(referenceId);
                if (booking == null)
                {
                    Console.WriteLine($"❌ FAILED: Booking with ID {referenceId} NOT FOUND in Database!");
                    return;
                }

                Console.WriteLine($"✅ Found Booking for User: {booking.UserId}. Updating now...");
                if (booking != null)
                {
                    booking.PaymentStatus = "Completed";
                    booking.BookingStatus = "Confirmed";
                    booking.TransactionId = intent.Id;
                    var user = clsUsers.Find(booking.UserId);
                    if (user != null) await booking.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                    isSaved = true;
                }
            }

           
            if (isSaved)
            {
                clsPayments payment = new clsPayments
                {
                    ReferenceID = referenceId,
                    ReferenceType = type, 
                    Amount = (decimal)intent.Amount / 100,
                    Currency = intent.Currency.ToUpper(),
                    PaymentMethod = intent.PaymentMethodTypes.FirstOrDefault() ?? "card",
                    TransactionID = intent.Id,
                    PaymentStatus = "Completed",
                    CreatedAt = DateTime.Now
                };
                await payment.Save();
            }
        }
    }
}
