using CoworkingSpace.BLL;
using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingSpace.API.Controllers
{
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
                  
                    return Ok(new
                    {
                        BookingId = spaceBookings.Id,
                        Amount = spaceBookings.TotalPrice,
                        ReferenceType = "Space",
                        Message = "Booking created, awaiting payment."
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
                var bookingTicketDetails =  clsSpaceBookings.Find(Id);
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
                var existingBookingTicket =  clsSpaceBookings.Find(id);
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

    }
}
