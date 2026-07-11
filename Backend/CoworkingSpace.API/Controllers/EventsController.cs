using CoworkingSpace.BLL;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase // Inherit from ControllerBase for API controllers
    {

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] eventModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("Event data is required.");
                }

                clsEvents events = new clsEvents
                {
                    Title = model.Title,
                    Description = model.Description,
                    EventDate = model.EventDate,
                    TicketPrice = model.TicketPrice,
                    MaxAttendees = model.MaxAttendees,
                    AvailableSeats = model.MaxAttendees // Assuming all seats are available initially, you can adjust this logic as needed
                };

                bool isSaved = await events.Save();
                if (!isSaved)
                {
                    return StatusCode(500, "An error occurred while saving the eventDetails.");
                }

                return Ok("Event added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error:During add the eventDetails.");
            }

        }

        [HttpGet("getAll")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var events = await clsEvents.GetAllEvents();
                if (events == null || events.Count == 0)
                    return NotFound("No events.");

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
                    return BadRequest("Event ID is required.");
                }

                var isDeleted = await clsEvents.Delete(Id);
                if (!isDeleted)
                {
                    return NotFound("Event not found or could not be deleted.");
                }
                return Ok(new { message = "Deleted successfully ", eventId = Id });
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Error:During delete the eventDetails.");
            }
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id == 0) 
                {
                    return BadRequest("Event ID is required.");
                }
                var eventDetails = clsEvents.Find(id); 
                if (eventDetails == null)
                {
                    return NotFound("Event not found.");
                }
                return Ok(eventDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}"); 
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int Id, [FromBody] eventModel model)
        {
            try
            {
                if (Id == 0 || model == null)
                {
                    return BadRequest("Event ID and data are required.");
                }
                var existingEvent = clsEvents.Find(Id);
                if (existingEvent == null)
                {
                    return NotFound("Event not found.");
                }
                existingEvent.Title = model.Title;
                existingEvent.Description = model.Description;
                existingEvent.EventDate = model.EventDate;
                existingEvent.TicketPrice = model.TicketPrice;
                existingEvent.MaxAttendees = model.MaxAttendees;
                existingEvent.AvailableSeats = model.AvailableSeats;
                bool isUpdated = await existingEvent.Save();
                if (!isUpdated)
                {
                    return StatusCode(500, "An error occurred while updating the eventDetails.");
                }
                return Ok("Event updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error:During update the eventDetails.");
            }


        }

        [HttpGet("upcoming-events")]
        public async  Task<IActionResult> GetUpcomingEventsCount()
        {
            try
            {
                int? countUpcomingEvents = await clsEvents.getUpcomingEventsCount();
                return Ok(new { countUpcomingEvents });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while return the count Upcoming Events.", error = ex.Message });
            }
        }

    }

}