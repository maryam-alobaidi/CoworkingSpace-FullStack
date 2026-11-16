using CoworkingSpace.API.Security;
using CoworkingSpace.BLL;
using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Authorization;
using CoworkingSpace.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;


namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventTicketsController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public EventTicketsController(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        // Where Stripe sends the customer back after checkout. Config "Frontend:BaseUrl" (env Frontend__BaseUrl).
        private string FrontendUrl => (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');



            [HttpPost("add")]
            public async Task<IActionResult> Add([FromBody] CreateTicketEventModel model)
            {
                try
                {
                    if (model == null) return BadRequest("Ticket booking data is required.");
                    if (model.Quantity <= 0) return BadRequest("Quantity must be at least 1.");
                    if (!User.CanAccessUser(model.UserId)) return Forbid();

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
                        // أ: إذا كانت الفعالية مجانية
                        if (eventDetails.TicketPrice == 0)
                        {
                            clsEventTickets singleTicketFree = new clsEventTickets
                            {
                                EventId = model.EventId,
                                UserId = model.UserId,
                                PurchaseDate = DateTime.Now,
                                PaymentStatus = "Complete" // مجانية ومكتملة فوراً
                            };

                            if (await singleTicketFree.Save())
                            {
                                savedTicketIds.Add(singleTicketFree.Id);
                            }

                            // 🌟 حل مشكلة التكرار: نستخدم continue لكي ننتقل للخطوة التالية في الـ Loop ونمنع إنشاء تذكرة Pending
                            continue;
                        }

                        // ب: إذا كانت الفعالية مدفوعة (يتم إنشاء تذكرة معلقة بالدفع)
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

                        // 🌟 3. إذا كانت الفعالية مجانية، نتخطى Stripe بالكامل ونرجع نجاح فوري
                        if (eventDetails.TicketPrice == 0)
                        {
                            // تقليص المقاعد المتاحة فوراً لأن التذاكر المجانية تم تأكيدها واكتملت
                            eventDetails.AvailableSeats -= model.Quantity;
                            await eventDetails.Save();

                            return Ok(new
                            {
                                EventTicketId = primaryTicketId,
                                TicketIds = savedTicketIds,
                                Amount = 0,
                                ReferenceType = "Event",
                                SessionUrl = "", // نرسل رابط فارغ لكي يعلم الأنجولار أنه لا يوجد دفع Stripe
                                Message = $"{model.Quantity} Free ticket bookings created and confirmed successfully!"
                            });
                        }

                        // 🌟 4. إذا كانت الفعالية مدفوعة، نقوم بتجهيز بوابة Stripe كالمعتاد
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
                            SuccessUrl = $"{FrontendUrl}/payment-success?ticketIds={ticketIdsString}&referenceType=Event&qty={model.Quantity}&session_id={{CHECKOUT_SESSION_ID}}",
                            CancelUrl = $"{FrontendUrl}/payment-failed?ticketId={primaryTicketId}",
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
                            SessionUrl = session.Url, // يمرر الرابط للأنجولار ليقوم بالتحويل لصفحة الدفع
                            Message = $"{model.Quantity} Ticket bookings created successfully, redirecting to payment."
                        });
                    }

                    return StatusCode(500, "Could not save the ticket bookings.");
                }
                catch (Exception ex)
                {
                    return this.ServerError(ex, "An error occurred while processing the ticket payment.");
                }
            }

            [HttpPost("confirm-payment")]
            public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmEventPaymentModel model)
            {
                try
                {
                    if (model == null) return BadRequest("Confirmation data is required.");
                    if (string.IsNullOrEmpty(model.TicketIds)) return BadRequest("No ticket IDs provided.");

                    // تفكيك أرقام الـ IDs والتأكد من تحويلها لأرقام برمجية آمنة
                    var ticketIdList = model.TicketIds.Split(',')
                                                      .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0)
                                                      .Where(id => id > 0)
                                                      .ToList();

                    if (!ticketIdList.Any()) return BadRequest("Invalid ticket IDs format.");

                    // Never trust the client's word that it paid: look the Checkout Session up in Stripe
                    // and make sure it is paid and was created for exactly these tickets.
                    if (string.IsNullOrEmpty(model.TransactionId) || !model.TransactionId.StartsWith("cs_"))
                        return BadRequest("A valid Stripe checkout session id is required.");

                    Stripe.Checkout.Session session;
                    try
                    {
                        session = await new Stripe.Checkout.SessionService().GetAsync(model.TransactionId);
                    }
                    catch (Stripe.StripeException)
                    {
                        return BadRequest("Stripe checkout session not found.");
                    }

                    if (session.PaymentStatus != "paid")
                        return BadRequest("Payment has not been completed.");

                    var paidTicketIds = (session.Metadata != null && session.Metadata.TryGetValue("TicketIds", out var rawPaidIds) ? rawPaidIds : "")
                        .Split(',')
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0)
                        .Where(id => id > 0)
                        .ToHashSet();

                    if (!ticketIdList.All(paidTicketIds.Contains))
                        return BadRequest("These tickets do not belong to the given payment.");

                    string verifiedTransactionId = session.PaymentIntentId ?? session.Id;

                    int eventId = 0;
                    int userId = 0;
                    bool processingSuccessful = false;
                    clsEventTickets businessTicket = null;

                    foreach (int ticketId in ticketIdList)
                    {
                        var ticketModel = await clsEventTickets.FindWithReturnclass(ticketId);

                        if (ticketModel != null)
                        {
                            if (!User.CanAccessUser(ticketModel.UserId)) return Forbid();

                            eventId = ticketModel.EventId;
                            userId = ticketModel.UserId;

                            // 🌟 السيناريو الأول: التذكرة ما زالت معلقة (Pending) - نقوم بتحديثها فوراً
                            if (ticketModel.PaymentStatus == "Pending")
                            {
                                businessTicket = new clsEventTickets
                                {
                                    Id = ticketModel.Id,
                                    EventId = ticketModel.EventId,
                                    UserId = ticketModel.UserId,
                                    PurchaseDate = ticketModel.PurchaseDate == default(DateTime) ? DateTime.Now : ticketModel.PurchaseDate,
                                    PaymentStatus = "Complete",
                                    TransactionId = verifiedTransactionId,
                                    Mode = clsEventTickets.enMode.update
                                };

                                if (await businessTicket.Save())
                                {
                                    processingSuccessful = true;
                                }
                            }
                            // 🌟 السيناريو الثاني: الـ Webhook كان أسرع وقام بتفعيلها لـ Completed مسبقاً
                            else if (ticketModel.PaymentStatus == "Completed" || ticketModel.PaymentStatus == "Complete")
                            {
                                processingSuccessful = true;
                                businessTicket = ticketModel; // نجهز الكائن لغايات الإيميل الاحتياطي
                            }
                        }
                    }

                    if (processingSuccessful)
                    {
                        // جلب الفعالية لتحديث المقاعد المتاحة (إن لم يتم تحديثها سابقاً)
                        if (eventId > 0)
                        {
                            var currentEvent = clsEvents.Find(eventId);
                            if (currentEvent != null)
                            {
                                int quantityToDeduct = model.Quantity > 0 ? model.Quantity : ticketIdList.Count;
                                // فكي التعليق بالأسفل إذا كنتِ تعتمدين على تفعيل خصم المقاعد لحظة تأكيد الدفع فقط:
                                // currentEvent.AvailableSeats = currentEvent.AvailableSeats - quantityToDeduct;
                                // await currentEvent.Save();
                            }
                        }

                        // إرسال الإيميل للمستخدم بأمان دون إفساد معاملة النجاح العامة
                        if (userId > 0 && businessTicket != null)
                        {
                            try
                            {
                                var user = clsUsers.Find(userId);
                                if (user != null)
                                {
                                    // استدعاء دالة بناء التذاكر وإرسالها المضمنة بداخل كلاس البزنس الخاص بكِ
                                    await businessTicket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                                }
                            }
                            catch (Exception emailEx)
                            {
                                // نسجل الخطأ في الكونسول لتلافي فشل إرجاع النجاح الكلي للمستخدم بسبب سيرفر الإيميل
                                Console.WriteLine($"⚠️ Email sending failed in ConfirmPayment: {emailEx.Message}");
                            }
                        }

                        return Ok(new { message = $"{ticketIdList.Count} Tickets confirmed successfully (either newly or via webhook)." });
                    }

                    return StatusCode(400, "Tickets not found or status could not be verified.");
                }
                catch (Exception ex)
                {
                    return this.ServerError(ex, "Error during payment confirmation.");
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
                if (!User.CanAccessUser(eventTicket.UserId)) return Forbid();

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
                    SuccessUrl = $"{FrontendUrl}/payment-success?ticketIds={eventTicket.Id}&referenceType=Event&qty=1&session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{FrontendUrl}/payment-failed?ticketId={eventTicket.Id}",

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
                return this.ServerError(ex, "An error occurred while processing the ticket payment.");
            }
        }



        [Authorize(Roles = Roles.Admin)]
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



        [Authorize(Roles = Roles.Admin)]
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
                if (!User.CanAccessUser(eventTicketDetails.UserId)) return Forbid();

                return Ok(eventTicketDetails);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error:During get the event Ticket details.");
            }
        }

        [Authorize(Roles = Roles.Admin)]
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
                return this.ServerError(ex, "An error occurred while updating the event ticket.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTickets(int userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();
            try
            {
                var tickets = await clsEventTickets.GetTicketsByUserId(userId);
                if (tickets == null || !tickets.Any()) return Ok(new List<object>());

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex, "An error occurred while loading event tickets.");
            }
        }

        [Authorize(Roles = Roles.Admin)]
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
                return this.ServerError(ex, "An error occurred while loading event tickets.");
            }
        }
    }
}