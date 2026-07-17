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
                    return BadRequest(new { message = "Event data is required." });
                }

                clsEvents events = new clsEvents
                {
                    Title = model.Title,
                    Description = model.Description,
                    EventDate = model.EventDate,
                    TicketPrice = model.TicketPrice,
                    MaxAttendees = model.MaxAttendees,
                    AvailableSeats = model.MaxAttendees
                };

                bool isSaved = await events.Save();
                if (!isSaved)
                {
                    return StatusCode(500, new { message = "An error occurred while saving the event details." });
                }

             
                model.Id = events.Id; 
                model.AvailableSeats = events.AvailableSeats;

                
                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error during adding the event details.", details = ex.Message });
            }
        }

        [HttpGet("getAllUpcomingEvents")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var events = await clsEvents.GetAllEvents();
                if (events == null || events.Count == 0)
                    return NotFound("No events.");
              
                        var today = DateTime.Today;

                var activeEvents = events
                    .Where(e => e.EventDate >= today)
                    .OrderBy(e => e.EventDate);

                        return Ok(activeEvents);   
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Error:During get the data.");
            }
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
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

                return StatusCode(500, $"Error: {ex.Message} -> {ex.InnerException?.Message}");
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
                    return BadRequest(new { message = "Event ID and data are required." });
                }

                // جلب الفعالية الحالية من قاعدة البيانات
                var existingEvent =  clsEvents.Find(Id);
                if (existingEvent == null)
                {
                    return NotFound(new { message = "Event not found." });
                }

                // تحديث الحقول الأساسية
                existingEvent.Title = model.Title;
                existingEvent.Description = model.Description;
                existingEvent.EventDate = model.EventDate;
                existingEvent.TicketPrice = model.TicketPrice;

                // حساب المقاعد المتاحة بذكاء عند تغيير السعة القصوى
                int bookedSeats = existingEvent.MaxAttendees - existingEvent.AvailableSeats;
                existingEvent.MaxAttendees = model.MaxAttendees;
                existingEvent.AvailableSeats = model.MaxAttendees - bookedSeats;

                // حفظ التعديلات في قاعدة البيانات
                bool isUpdated = await existingEvent.Save();
                if (!isUpdated)
                {
                    return StatusCode(500, new { message = "An error occurred while updating the event details." });
                }

                // إرجاع كائن JSON لتجنب مشاكل الـ Parsing في الأنجولار
                return Ok(new { message = "Event updated successfully.", id = Id });
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ الحقيقي لمساعدتك في الـ Debugging
                return StatusCode(500, new { message = $"Error during update: {ex.Message}" });
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