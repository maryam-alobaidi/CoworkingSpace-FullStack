using CoworkingSpace.BLL;
using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Writers;
using System.Transactions;
using static System.Formats.Asn1.AsnWriter;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventTicketsController: ControllerBase
    {

        private readonly IEmailService _emailService;

        public EventTicketsController(IEmailService emailService)
        {
            _emailService = emailService;
        }


        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] CreateTicketEventModel model)
        {
            try
            {
                if (model == null) return BadRequest("Ticket booking data is required.");

                var user = clsUsers.Find(model.UserId);
                if (user == null) return NotFound("User not found.");

                var eventDetails = clsEvents.Find(model.EventId);
                if (eventDetails == null) return NotFound("Event not found.");

                // 🌟 1. حساب السعر الإجمالي الكلي للعملية
                decimal totalCalculatedPrice = eventDetails.TicketPrice * model.Quantity;

                // قائمة للاحتفاظ بـ IDs التذاكر التي سيتم إنشاؤها
                List<int> savedTicketIds = new List<int>();

                // 🌟 2. السحر هنا: تكرار عملية الحفظ بناءً على الكمية المطلوبة (For Loop)
                for (int i = 0; i < model.Quantity; i++)
                {
                    clsEventTickets singleTicket = new clsEventTickets
                    {
                        EventId = model.EventId,
                        UserId = model.UserId,
                        PurchaseDate = DateTime.Now,
                        PaymentStatus = "Pending"
                        // الـ TicketCode والـ TransactionId يتم تحديثهم في دالة النجاح (Success) لاحقاً
                    };

                    if (await singleTicket.Save())
                    {
                        savedTicketIds.Add(singleTicket.Id);
                    }
                }

                // التحقق من نجاح حفظ التذاكر بنجاح
                if (savedTicketIds.Any())
                {
                    // نأخذ معرف أول تذكرة كمرجع احتياطي
                    int primaryTicketId = savedTicketIds.First();

                    // تحويل مصفوفة الـ IDs إلى نص مفصول بفاصلة (مثال: "12,13,14") لتمريره لـ Stripe
                    string ticketIdsString = string.Join(",", savedTicketIds);

                    // 🌟 3. إعداد جلسة Stripe ممرر بها الكمية والسعر بشكل احترافي
                    var options = new Stripe.Checkout.SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(eventDetails.TicketPrice * 100), // سعر التذكرة الواحدة بالسنتات
                            Currency = "usd",
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Tickets for: {eventDetails.Title}",
                                Description = $"You are purchasing {model.Quantity} individual tickets."
                            },
                        },
                        Quantity = model.Quantity, // 🌟 نمرر الكمية الحقيقية لـ Stripe ليراها العميل بوضوح بصفحة الدفع
                    },
                },
                        Mode = "payment",

                        // نرسل كل الـ ticketIds بداخل الرابط لكي تستقبلهم دالة النجاح في الفرونت اند والباك اند وتحدث حالتهم جميعاً إلى Completed
                        SuccessUrl = $"http://localhost:4200/payment-success?ticketIds={ticketIdsString}&referenceType=Event",
                        CancelUrl = $"http://localhost:4200/payment-failed?ticketId={primaryTicketId}",

                        Metadata = new Dictionary<string, string>
                {
                    { "Type", "Event" },
                    { "TicketIds", ticketIdsString }, // الاحتفاظ بالمعرفات داخل بيانات Stripe للمراجعة
                    { "Quantity", model.Quantity.ToString() }
                }
                    };

                    var service = new Stripe.Checkout.SessionService();
                    Stripe.Checkout.Session session = await service.CreateAsync(options);

                    // 🌟 4. إرجاع النتيجة المتكاملة للأنجولار للتحويل لصفحة الدفع
                    return Ok(new
                    {
                        EventTicketId = primaryTicketId, // المعرف الرئيسي
                        TicketIds = savedTicketIds,      // مصفوفة بكل المعرفات المحجوزة سوياً
                        Amount = totalCalculatedPrice,
                        ReferenceType = "Event",
                        SessionUrl = session.Url,
                        Message = $"{model.Quantity} Ticket bookings created successfully, redirecting to payment."
                    });
                }

                return StatusCode(500, "Could not save the ticket bookings.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }




        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmEventPaymentModel model)
        {
            try
            {
                if (model == null) return BadRequest("Confirmation data is required.");

                // 1. جلب التذكرة المعلقة بناءً على الـ ID القادم من الأنجولار
                var ticket = await clsEventTickets.Find(model.TicketId);
                if (ticket == null) return NotFound("Ticket not found.");

                // 2. تحديث بيانات التذكرة لتصبح مكتملة
                ticket.PaymentStatus = "Completed";
                ticket.TransactionId = model.TransactionId; // الكود القادم من Stripe
                ticket.Mode = clsEventTickets.enMode.update;
                if (await ticket.Save())
                {
                    // 3. 🌟 جلب كائن الـ Event بكامل بياناته لتجنب فقدان أي حقل
                    var currentEvent = clsEvents.Find(ticket.EventId);
                    if (currentEvent != null)
                    {
                        // تنقيص عدد المقاعد المتاحة بناءً على الكمية
                        currentEvent.AvailableSeats = currentEvent.AvailableSeats - model.Quantity;

                        // استدعاء دالة التحديث التي تعتمد على الـ Stored Procedure الخاص بكِ
                        // تأكدي من أن كلاس clsEvents يحتوي على طريقة لتحديث الكائن بالكامل
                        await currentEvent.Save();
                    }

                    // 4. إرسال الإيميل للمستخدم بعد نجاح العملية بالكامل
                    var user = clsUsers.Find(ticket.UserId);
                    if (user != null)
                    {
                        await ticket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                    }

                    return Ok(new { message = "Payment confirmed, seats updated, and email sent successfully." });
                }

                return StatusCode(500, "Could not update ticket status.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error during confirmation: {ex.Message}");
            }
        }


        [HttpGet("getAll")]
        public async Task<IActionResult> Get()
        {
            try
            {
              
                var events = await clsEventTickets.GetAll();
                if (events == null || events.Count == 0)
                    return NotFound("No events ticket.");

                return Ok(events);
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Error:During get the data.");
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            try
            {
                if (Id == 0)
                {
                    return BadRequest("Event Ticket ID is required.");
                }

                var isDeleted = await clsEventTickets.Delete(Id);
                if (!isDeleted)
                {
                    return NotFound("EventTicket not found or could not be deleted.");
                }
                return Ok(new { message = "Deleted successfully ", Id = Id });
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Error:During delete the eventDetails.");
            }
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(int Id)
        {
            try
            {
                if (Id == 0)
                {
                    return BadRequest("Event Ticket ID is required.");
                }
                var eventTicketDetails = await clsEventTickets.Find(Id);
                if (eventTicketDetails==null)
                {
                    return NotFound("Event Ticket not found.");
                }
                return Ok(eventTicketDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error:During get the event Ticket details.");
            }


        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTicketEventModel model)
        {
            try
            {
                if (id == 0 || model == null)
                {
                    return BadRequest("Event Ticket ID and data are required.");
                }
                model.Id = id;
                var existingEventTicket = await clsEventTickets.Find(id);
                if (existingEventTicket==null )
                {
                    return NotFound("Event Ticket not found.");
                }
               
                existingEventTicket.EventId = model.EventId;
                existingEventTicket.UserId = model.UserId;

                var user =  clsUsers.Find(existingEventTicket.UserId);



                bool isAllGood = await existingEventTicket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                if (!isAllGood)
                {
                    return StatusCode(500, "An error occurred while saving the event ticket Details or sending the email.");
                }

              
                return Ok("Event updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error:During update the event ticket Details.");
            }


        }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTickets(int userId)
        {
            try
            {
              
                var tickets = await clsEventTickets.GetTicketsByUserId(userId);

                if (tickets == null || !tickets.Any())
                {
                 
                    return Ok(new List<object>());
                }

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
