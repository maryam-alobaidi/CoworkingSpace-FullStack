using CoworkingSpace.BLL;
using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Collections.Generic;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/SpaceBookings")] 
    [ApiController]
    public class SpaceBookingsController : Controller
    {

        private readonly IEmailService _emailService;
        

        public SpaceBookingsController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] CreateSpaceBookingModel model)
        {
            try
            {
                if (model == null) return BadRequest("Booking data is required.");

                var user = clsUsers.Find(model.UserId);
                if (user == null) return NotFound("User not found.");

                var space = await clsWorkspaceSpaces.Find(model.SpaceId);
                if (space == null) return NotFound("Space not found.");

                decimal pricePerHour = space.PricePerHour;
                var start = TimeSpan.Parse(model.StartTime);
                var end = TimeSpan.Parse(model.EndTime);
                decimal hours = (decimal)(end - start).TotalHours;
                decimal calculatedPrice = hours * pricePerHour;

                clsSpaceBookings spaceBookings = new clsSpaceBookings
                {
                    UserId = model.UserId,
                    SpaceId = model.SpaceId,
                    BookingDate = model.BookingDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    TotalPrice = calculatedPrice,
                    BookingStatus = "Pending",
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.Now
                };

                if (await spaceBookings.Save())
                {
                  

                    var options = new Stripe.Checkout.SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                    {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(spaceBookings.TotalPrice * 100), // in centat
                            Currency = "usd",
                            
                            
                            ProductData = new()
                            {
                                Name = $"Workspace Booking #{spaceBookings.Id}",
                                Description = $"Date: {spaceBookings.BookingDate:yyyy-MM-dd}"
                            },
                            },
                        Quantity = 1,
                    },
                },
                        Mode = "payment",
                        // 🌟 هنا السحر! نخبر Stripe عندما ينجح العميل في الدفع، أعده تلقائياً إلى هذا الرابط في الأنجولار
                        SuccessUrl = "http://localhost:4200/payment-success?bookingId=" + spaceBookings.Id,
                        CancelUrl = "http://localhost:4200/payment-failed?bookingId=" + spaceBookings.Id,

                        Metadata = new Dictionary<string, string>
                {
                    { "Type", "Space" },
                    { "BookingId", spaceBookings.Id.ToString() }
                }
                    };

                    var service = new Stripe.Checkout.SessionService();
                    Stripe.Checkout.Session session = await service.CreateAsync(options);

                    return Ok(new
                    {
                        BookingId = spaceBookings.Id,
                        Amount = spaceBookings.TotalPrice,
                        ReferenceType = "Space",
                        SessionUrl = session.Url, // الرابط جاهز لينطلق للأنجولار
                        Message = "Booking created, redirecting to payment."
                    });
                }

                return StatusCode(500, "Could not save the booking.");
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
                var booking = await clsSpaceBookings.GetAllSpaceBookings();
                if (booking == null || booking.Count == 0)
                    return NotFound("No booking ticket.");

                return Ok(booking);
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
                    return BadRequest("Booking Ticket ID is required.");
                }

                var isDeleted = await clsSpaceBookings.Delete(Id);
                if (!isDeleted)
                {
                    return NotFound("Booking not found or could not be deleted.");
                }
                return Ok(new { message = "Deleted successfully ", Id = Id });
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Error:During delete the Booking Details.");
            }
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(int Id)
        {
            try
            {
                if (Id == 0)
                {
                    return BadRequest("Bookingc Ticket ID is required.");
                }
                var bookingTicketDetails = clsSpaceBookings.Find(Id);
                if (bookingTicketDetails == null)
                {
                    return NotFound("Booking Ticket not found.");
                }
                return Ok(bookingTicketDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error:During get the Booking Ticket details.");
            }


        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSpaceBookingModel model)
        {
            try
            {
                if (id == 0 || model == null)
                {
                    return BadRequest("Booking Ticket ID and data are required.");
                }
                model.SpaceId = id;
                var existingBookingTicket = clsSpaceBookings.Find(id);
                if (existingBookingTicket == null)
                {
                    return NotFound("Booking Ticket not found.");
                }

                existingBookingTicket.SpaceId = model.SpaceId;
                existingBookingTicket.UserId = model.UserId;

                var user = clsUsers.Find(existingBookingTicket.UserId);



                bool isAllGood = await existingBookingTicket.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                if (!isAllGood)
                {
                    return StatusCode(500, "An error occurred while saving the Booking ticket Details or sending the email.");
                }


                return Ok("Booking updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error:During update the Booking ticket Details.");
            }


        }


        [HttpGet("GetBookedSlots")]
        public async Task<IActionResult> GetBookedSlots(int spaceId, DateTime bookingDate)
        {
            try
            {
                if (spaceId <= 0)
                {
                    return BadRequest("Space ID is required.");
                }
                var bookedSlots = await clsSpaceBookings.GetBookedSlots(spaceId, bookingDate);
                return Ok(bookedSlots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: During fetching booked slots.");
            }
        }


        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserBookings(int id)
        {
            try
            {
               
                if (id <= 0)
                {
                    return BadRequest("Valid User ID is required.");
                }


                List <spaceBookingsModel> bookings = await clsSpaceBookings.getUserBooking(id);

                
                if (bookings == null || !bookings.Any())
                {
                    return Ok(new List<spaceBookingsModel>()); 
                }

                
                return Ok(bookings);
            }
            catch (Exception ex)
            {
               
               
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("repay/{Id}")]
        public async Task<IActionResult> Repay(int Id)
        {
            try
            {
                var spaceBooking = clsSpaceBookings.Find(Id);
                if (spaceBooking==null)
                {
                    return NotFound("Id not found.");
                }


                if (spaceBooking.PaymentStatus != "Pending")
                {
                    return BadRequest("This booking cannot be paid.");

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
                        UnitAmount = (long)(spaceBooking.TotalPrice * 100), 
                        Currency = "usd",
                        ProductData = new()
                        {
                            Name = $"Workspace Booking #{spaceBooking.Id} (Repayment)",
                            Description = $"Date: {spaceBooking.BookingDate:yyyy-MM-dd}"
                        },
                    },
                    Quantity = 1,
                },
            },
                    Mode = "payment",
                    SuccessUrl = "http://localhost:4200/payment-success?bookingId=" + spaceBooking.Id,
                    CancelUrl = "http://localhost:4200/payment-failed?bookingId=" + spaceBooking.Id,
                    Metadata = new Dictionary<string, string>
            {
                { "Type", "Space" },
                { "BookingId", spaceBooking.Id.ToString() }
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


        [HttpGet("active-bookings")]
        public async Task<IActionResult> getActiveBookings()
        {
            try
            {
                int? countActiveBookings = await clsSpaceBookings.getActiveBookings();
                return Ok(new { countActiveBookings });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while return active bookings.", error = ex.Message });
            }
        }


        [HttpGet ("recent-reservation")]
        public async Task<IActionResult> getRecentSpaceReservations()
        {
            try
            {
                var recentBookings = await clsSpaceBookings.getRecentSpaceReservations();
                return Ok( recentBookings );

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while returning recent bookings.", error = ex.Message });
            }
        }

    }
}
