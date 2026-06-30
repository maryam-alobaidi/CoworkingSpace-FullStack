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
        // هذا السر ستحصلين عليه عند تشغيل Stripe CLI لاجل الترجربه عمليه الدفع المزيفه عليه

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
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], endpointSecret);

                
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    // 🌟 2. تحويل الكائن القادم إلى Session بدلاً من PaymentIntent
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;


                    string type = session.Metadata.ContainsKey("Type") ? session.Metadata["Type"] : "";
                    int referenceId = 0;

                    if (type == "Space")
                    {
                        referenceId = int.Parse(session.Metadata["BookingId"]);
                    }
                    else if (type == "Event")
                    {
                        referenceId = int.Parse(session.Metadata["TicketId"]);
                    }

                    if (referenceId > 0)
                    {
                       
                        var mockIntent = new PaymentIntent
                        {
                            Id = session.PaymentIntentId ?? session.Id,
                            Amount = session.AmountTotal ?? 0,
                            Currency = session.Currency ?? "usd",
                            PaymentMethodTypes = session.PaymentMethodTypes ?? new List<string> { "card" }
                        };

                        await _HandleSuccessfulPayment(referenceId, type, mockIntent);
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
                var ticket = await clsEventTickets.Find(referenceId);
                if (ticket != null)
                {
                    ticket.PaymentStatus = "Completed";
                    ticket.TransactionId = intent.Id;
                    var user = clsUsers.Find(ticket.UserId);
                    if (user != null) await ticket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                    isSaved = true;

                    // 🔔 إضافة الإشعار الخاص بالتذاكر وتوجيهه إلى مسار الأنجولار الجديد
                    clsNotifications notifications = new clsNotifications
                    {
                        UserID = ticket.UserId,
                        Title = "Event Ticket Purchased 🎟️",
                        Message = $"Your Ticket for the event has been successfully purchased.",
                        NotificationType = "EventTicket",
                        TargetURL = "/dashboard/event-tickets", 
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        ReadAt = null,
                        Mode = clsNotifications.enMode.addNew
                    };

                    await notifications.AddNewNotifications();
                }
            }

            // 2️⃣ ثانياً: حالة حجز مساحات العمل والطاولات (Space)
            if (type == "Space")
            {
                var booking = clsSpaceBookings.Find(referenceId);
                if (booking == null)
                {
                    Console.WriteLine($"❌ FAILED: Booking with ID {referenceId} NOT FOUND in Database!");
                    return;
                }

                Console.WriteLine($"✅ Found Booking for User: {booking.UserId}. Updating now...");

                booking.PaymentStatus = "Completed";
                booking.BookingStatus = "Confirmed";
                booking.TransactionId = intent.Id;
                var user = clsUsers.Find(booking.UserId);
                if (user != null) await booking.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                isSaved = true;

                // 🔔 🌟 هنا الجزء الجديد: توليد إشعار فخم ومستقل لحجز المساحة وتوجيهه للنظام الجديد
                clsNotifications notifications = new clsNotifications
                {
                    UserID = booking.UserId,
                    Title = "Space Booking Confirmed 🏢",
                    Message = $"Your reservation for Space #{booking.SpaceId} has been successfully confirmed.",
                    NotificationType = "SpaceBooking",
                    TargetURL = "/dashboard/office-bookings", // 🌟 تم التوجيه لمكون الـ Office Bookings
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    ReadAt = null,
                    Mode = clsNotifications.enMode.addNew
                };

                await notifications.AddNewNotifications();
            }


            // 3️⃣ ثالثاً: تسجيل عملية الدفع في قاعدة البيانات (Payments Table)
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
