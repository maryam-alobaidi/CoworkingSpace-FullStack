using CoworkingSpace.BLL;
using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Mvc;


namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventTicketsController : ControllerBase
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

                // 1. حساب السعر الإجمالي الكلي للعملية
                decimal totalCalculatedPrice = eventDetails.TicketPrice * model.Quantity;

                // قائمة للاحتفاظ بـ IDs التذاكر التي سيتم إنشاؤها
                List<int> savedTicketIds = new List<int>();

                // 2. تكرار عملية الحفظ بناءً على الكمية المطلوبة
                for (int i = 0; i < model.Quantity; i++)
                {
                    clsEventTickets singleTicket = new clsEventTickets
                    {
                        EventId = model.EventId,
                        UserId = model.UserId,
                        PurchaseDate = DateTime.Now,
                        PaymentStatus = "Pending"
                    };

                    if (await singleTicket.Save())
                    {
                        savedTicketIds.Add(singleTicket.Id);
                    }
                }

                if (savedTicketIds.Any())
                {
                    int primaryTicketId = savedTicketIds.First();
                    string ticketIdsString = string.Join(",", savedTicketIds);

                    // 3. إعداد جلسة Stripe ممرر بها الكمية والسعر بشكل احترافي
                    var options = new Stripe.Checkout.SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                        {
                            new Stripe.Checkout.SessionLineItemOptions
                            {
                                PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                                {
                                    UnitAmount = (long)(eventDetails.TicketPrice * 100), // بالسنتات
                                    Currency = "usd",
                                    ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = $"Tickets for: {eventDetails.Title}",
                                        Description = $"You are purchasing {model.Quantity} individual tickets."
                                    },
                                },
                                Quantity = model.Quantity,
                            },
                        },
                        Mode = "payment",

                        // 🌟 تحديث رابط النجاح لتمرير كمية التذاكر الكلية بدقة للأنجولار
                        SuccessUrl = $"http://localhost:4200/payment-success?ticketIds={ticketIdsString}&referenceType=Event&qty={model.Quantity}",
                        CancelUrl = $"http://localhost:4200/payment-failed?ticketId={primaryTicketId}",

                        Metadata = new Dictionary<string, string>
                        {
                            { "Type", "Event" },
                            { "TicketIds", ticketIdsString },
                            { "Quantity", model.Quantity.ToString() }
                        }
                    };

                    var service = new Stripe.Checkout.SessionService();
                    Stripe.Checkout.Session session = await service.CreateAsync(options);

                    return Ok(new
                    {
                        EventTicketId = primaryTicketId,
                        TicketIds = savedTicketIds,
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
                if (string.IsNullOrEmpty(model.TicketIds)) return BadRequest("No ticket IDs provided.");

                var ticketIdList = model.TicketIds.Split(',')
                                                    .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0)
                                                    .Where(id => id > 0)
                                                    .ToList();

                if (!ticketIdList.Any()) return BadRequest("Invalid ticket IDs format.");

                int eventId = 0;
                int userId = 0;
                bool processingSuccessful = false;
                clsEventTickets businessTicket = null;

                foreach (int ticketId in ticketIdList)
                {
                    var ticketModel = await clsEventTickets.FindWithReturnclass(ticketId);

                    if (ticketModel != null)
                    {
                        eventId = ticketModel.EventId;
                        userId = ticketModel.UserId;

                        // 🌟 السيناريو الأول: التذكرة ما زالت معلقة (نقوم بتحديثها حالاً)
                        if (ticketModel.PaymentStatus == "Pending")
                        {
                            businessTicket = new clsEventTickets
                            {
                              
                               
                                Id = ticketModel.Id,
                                EventId = ticketModel.EventId,
                                UserId = ticketModel.UserId,
                                PurchaseDate = ticketModel.PurchaseDate == default(DateTime) ? DateTime.Now : ticketModel.PurchaseDate,
                                PaymentStatus = "Completed",
                                TransactionId = model.TransactionId,
                                Mode = clsEventTickets.enMode.update
                            };

                            if (await businessTicket.Save())
                            {
                                processingSuccessful = true;
                            }
                        }
                        // 🌟 السيناريو الثاني: الـ Webhook كان أسرع وحولها بالفعل إلى Completed
                        else if (ticketModel.PaymentStatus == "Completed")
                        {
                            // نعتبر العملية ناجحة تماماً ولا داعي لرمي خطأ للمستخدم
                            processingSuccessful = true;

                            // نجهز الكائن لغايات إرسال الإيميل الاحتياطي إن لزم الأمر
                            businessTicket = ticketModel;
                        }
                    }
                }

                // إذا تم التحديث بنجاح أو كانت التذاكر مدفوعة بالفعل ومؤكدة خلف الكواليس
                if (processingSuccessful)
                {
                    // تحديث المقاعد احتياطياً في حال لم يقم الـ Webhook بخصمها بعد
                    if (eventId > 0)
                    {
                        var currentEvent = clsEvents.Find(eventId);
                        if (currentEvent != null)
                        {
                            // نتحقق أولاً لمنع الخصم المزدوج للمقاعد إذا خصمها الـ Webhook
                            // (يمكنكِ تعديل الحسبة بناءً على منطق البزنس الخاص بكِ)
                            int quantityToDeduct = model.Quantity > 0 ? model.Quantity : ticketIdList.Count;

                            // هنا نخصم فقط إذا أردنا التأكيد الصارم من شاشة الفرونت إند
                            // currentEvent.AvailableSeats = currentEvent.AvailableSeats - quantityToDeduct;
                            // await currentEvent.Save();
                        }
                    }

                    // إرسال الإيميل بأمان
                    if (userId > 0 && businessTicket != null)
                    {
                        try
                        {
                            var user = clsUsers.Find(userId);
                            if (user != null)
                            {
                                await businessTicket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                            }
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"⚠️ Email logging failed in ConfirmPayment: {emailEx.Message}");
                        }
                    }

                    return Ok(new { message = $"{ticketIdList.Count} Tickets confirmed successfully (either newly or via webhook)." });
                }

                return StatusCode(400, "Tickets not found or status could not be verified.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 CRITICAL ERROR in ConfirmPayment: {ex.Message}");
                return StatusCode(500, $"Error during confirmation: {ex.Message}");
            }
        }




        [HttpPost("repay/{Id}")]
        public async Task<IActionResult> Repay(int Id)
        {
            try
            {
                var eventTicket = await clsEventTickets.Find(Id);
                if (eventTicket == null)
                {
                    return NotFound("Ticket ID not found.");
                }

                if (eventTicket.PaymentStatus != "Pending")
                {
                    return BadRequest("This event ticket cannot be paid because it is not pending.");
                }

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                    {
                        new Stripe.Checkout.SessionLineItemOptions
                        {
                            PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)((eventTicket.TotalPrice ?? 0) * 100),
                                Currency = "usd",
                                ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"{eventTicket.EventTitle ?? "Event Ticket"} #{eventTicket.Id} (Repayment)",
                                    Description = $"Original Booking Date: {eventTicket.PurchaseDate:yyyy-MM-dd}"
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",

                    // 🌟 إصلاح دالة الـ Repay: إرسال الكمية كـ 1 ثابتة بما أن إعادة الدفع تتم لتذكرة فرعية محددة ومفردة
                    SuccessUrl = $"http://localhost:4200/payment-success?ticketIds={eventTicket.Id}&referenceType=Event&qty=1",
                    CancelUrl = $"http://localhost:4200/payment-failed?ticketId={eventTicket.Id}",

                    Metadata = new Dictionary<string, string>
                    {
                        { "Type", "Event" },
                        { "TicketIds", eventTicket.Id.ToString() },
                        { "Quantity", "1" }
                    }
                };

                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = await service.CreateAsync(options);

                return Ok(new { SessionUrl = session.Url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
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
            catch (Exception)
            {
                return StatusCode(500, "Error:During get the data.");
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            try
            {
                if (Id == 0) return BadRequest("Event Ticket ID is required.");

                var isDeleted = await clsEventTickets.Delete(Id);
                if (!isDeleted) return NotFound("EventTicket not found or could not be deleted.");

                return Ok(new { message = "Deleted successfully ", Id = Id });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error:During delete the eventDetails.");
            }
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(int Id)
        {
            try
            {
                if (Id == 0) return BadRequest("Event Ticket ID is required.");

                var eventTicketDetails = await clsEventTickets.Find(Id);
                if (eventTicketDetails == null) return NotFound("Event Ticket not found.");

                return Ok(eventTicketDetails);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error:During get the event Ticket details.");
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTicketEventModel model)
        {
            try
            {
                if (id == 0 || model == null) return BadRequest("Event Ticket ID and data are required.");

                model.Id = id;
                var existingTicketModel = await clsEventTickets.Find(id);
                if (existingTicketModel == null) return NotFound("Event Ticket not found.");

                var user = clsUsers.Find(model.UserId);
                if (user == null) return NotFound("User not found.");

                clsEventTickets businessTicket = new clsEventTickets
                {
                    Id = id,
                    EventId = model.EventId,
                    UserId = model.UserId,
                    TicketCode = existingTicketModel.TicketCode,
                    PurchaseDate = existingTicketModel.PurchaseDate ?? DateTime.Now,
                    PaymentStatus = existingTicketModel.PaymentStatus,
                    TransactionId = existingTicketModel.TransactionId,
                    Mode = clsEventTickets.enMode.update
                };

                bool isAllGood = await businessTicket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);

                if (!isAllGood) return StatusCode(500, "An error occurred while saving the event ticket Details or sending the email.");

                return Ok("Event ticket updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error during updating the event ticket details: {ex.Message}");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTickets(int userId)
        {
            try
            {
                var tickets = await clsEventTickets.GetTicketsByUserId(userId);
                if (tickets == null || !tickets.Any()) return Ok(new List<object>());

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("recent-event-ticket")]
        public async Task<IActionResult> GetRecentEventTicket()
        {
            try
            {
                var recentTicket = await clsEventTickets.GetRecentEventTicket();
                if (recentTicket == null) return NotFound("No recent event ticket found.");
                return Ok(recentTicket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}