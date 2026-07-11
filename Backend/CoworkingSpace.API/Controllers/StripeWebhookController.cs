using CoworkingSpace.BLL;
using CoworkingSpace.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/webhook")]
    public class StripeWebhookController : Controller
    {
        // هذا السر ستحصلين عليه عند تشغيل Stripe CLI لأجل تجربة عملية الدفع المحلية
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
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                    string type = session.Metadata.ContainsKey("Type") ? session.Metadata["Type"] : "";

                    // جلب نص المعرفات بناءً على النوع للتأكد من مطابقة المفاتيح مع الـ Controller
                    string rawIds = "";
                    if (type == "Space" && session.Metadata.ContainsKey("BookingId"))
                    {
                        rawIds = session.Metadata["BookingId"];
                    }
                    else if (type == "Event" && session.Metadata.ContainsKey("TicketIds"))
                    {
                        rawIds = session.Metadata["TicketIds"]; // 🌟 استخدام المفتاح الجمع المطابق للـ Controller
                    }

                    if (!string.IsNullOrEmpty(rawIds))
                    {
                        var mockIntent = new PaymentIntent
                        {
                            Id = session.PaymentIntentId ?? session.Id,
                            Amount = session.AmountTotal ?? 0,
                            Currency = session.Currency ?? "usd",
                            PaymentMethodTypes = session.PaymentMethodTypes ?? new List<string> { "card" }
                        };

                        // تمرير النص الخام (الذي قد يحتوي على فواصل) للدالة المحدثة بالأسفل
                        await _HandleSuccessfulPayment(rawIds, type, mockIntent);
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

        private async Task _HandleSuccessfulPayment(string rawIds, string type, PaymentIntent intent)
        {
            bool isSaved = false;
            int primaryReferenceId = 0; // متغير للاحتفاظ بمعرف رئيسي لجدول المدفوعات الكلية

            // 1️⃣ بلوك معالجة الـ Event (يدعم تذكرة واحدة أو مجموعة تذاكر مفصولة بفاصلة)
            if (type == "Event")
            {
                // تفكيك النص القادم (مثل: "12,13,14") إلى مصفوفة أرقام
                var ticketIdList = rawIds.Split(',')
                                         .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0)
                                         .Where(id => id > 0)
                                         .ToList();

                if (ticketIdList.Any())
                {
                    primaryReferenceId = ticketIdList.First(); // تحديد أول تذكرة كمرجع للدفعة الكلية

                    // حلقة تكرارية لتحديث كل التذاكر المرتبطة بعملية الشراء هذه
                    foreach (int ticketId in ticketIdList)
                    {
                        var ticket = await clsEventTickets.FindWithReturnclass(ticketId);
                        if (ticket != null && ticket.PaymentStatus != "Completed")
                        {
                            ticket.PaymentStatus = "Completed";
                            ticket.TransactionId = intent.Id;
                            ticket.Mode = clsEventTickets.enMode.update;

                            // حفظ التحديث لكل تذكرة بشكل منفصل
                            await ticket.Save();

                            // إرسال إشعار داخلي للمستخدم بخصوص التذكرة
                            clsNotifications notifications = new clsNotifications
                            {
                                UserID = ticket.UserId,
                                Title = "Event Ticket Purchased 🎟️",
                                Message = "Your Ticket for the event has been successfully purchased.",
                                NotificationType = "EventTicket",
                                TargetURL = "/dashboard/event-tickets",
                                IsRead = false,
                                CreatedAt = DateTime.Now,
                                ReadAt = null,
                                Mode = clsNotifications.enMode.addNew
                            };
                            await notifications.AddNewNotifications();
                            isSaved = true;

                            // محاولة إرسال الإيميل اللوجي للمستخدم
                            try
                            {
                                var user = clsUsers.Find(ticket.UserId);
                                if (user != null)
                                {
                                    await ticket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                                }
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"⚠️ Event Email sending failed for Ticket #{ticketId}: {emailEx.Message}");
                            }
                        }
                    }
                }
            }

            // 2️⃣ بلوك معالجة الـ Space (الحجوزات الفردية للمساحات)
            if (type == "Space")
            {
                if (int.TryParse(rawIds, out int bookingId))
                {
                    primaryReferenceId = bookingId;
                    var booking = clsSpaceBookings.Find(bookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "Completed";
                        booking.BookingStatus = "Confirmed";
                        booking.TransactionId = intent.Id;

                        // حفظ التعديل للحجز
                        await booking.Save();

                        clsNotifications notifications = new clsNotifications
                        {
                            UserID = booking.UserId,
                            Title = "Space Booking Confirmed 🏢",
                            Message = $"Your reservation for Space #{booking.SpaceId} has been successfully confirmed.",
                            NotificationType = "SpaceBooking",
                            TargetURL = "/dashboard/office-bookings",
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            ReadAt = null,
                            Mode = clsNotifications.enMode.addNew
                        };

                        await notifications.AddNewNotifications();
                        isSaved = true;

                        try
                        {
                            var user = clsUsers.Find(booking.UserId);
                            if (user != null)
                                await booking.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"⚠️ Space Email sending failed: {emailEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ FAILED: Booking with ID {bookingId} NOT FOUND in Database!");
                        return;
                    }
                }
            }

            // 3️⃣ تسجيل عملية الدفع الإجمالية في جدول المدفوعات (clsPayments) بعد معالجة أي من النوعين
            if (isSaved && primaryReferenceId > 0)
            {
                clsPayments payment = new clsPayments
                {
                    ReferenceID = primaryReferenceId,
                    ReferenceType = type,
                    Amount = (decimal)intent.Amount / 100, // تحويل السنتات القادمة من Stripe إلى المبلغ الفعلي
                    Currency = intent.Currency.ToUpper(),
                    PaymentMethod = intent.PaymentMethodTypes.FirstOrDefault() ?? "card",
                    TransactionID = intent.Id,
                    PaymentStatus = "Completed",
                    CreatedAt = DateTime.Now,
                    Mode = clsPayments.enMode.addNew
                };

                await payment.Save();
            }
        }
    }
}