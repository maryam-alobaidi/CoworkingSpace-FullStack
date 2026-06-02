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
                if (model == null)
                {
                    return BadRequest("Event tickets data is required.");
                }

                var user = clsUsers.Find(model.UserId);
                if (user == null)
                {
                    return NotFound("User not found for the given UserId.");
                }


                clsEventTickets eventTickets = new clsEventTickets
                {
                    EventId = model.EventId,
                    UserId = model.UserId,
                    
                };
             
                bool isAllGood=await eventTickets.SaveTicketWithEmailLog(user.Email, user.FullName, _emailService);
                if (!isAllGood)
                {
                    return StatusCode(500, "An error occurred while saving the event ticket Details or sending the email.");
                }

               
                return Ok("Event ticket added successfully and sent the confirmation for user email.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error:During add the event ticket Details.");
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

    }
}
