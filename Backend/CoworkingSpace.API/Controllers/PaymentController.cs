using CoworkingSpace.Models;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentRequestModel request)
        {
            var metadata = new Dictionary<string, string>();

            // تحديد نوع المعرف المرجعي (تذكرة أم حجز مساحة)
            if (request.ReferenceType == "Event")
            {
                metadata.Add("TicketId", request.ReferenceID.ToString());
                metadata.Add("Type", "Event");
            }
            else if (request.ReferenceType == "Space")
            {
                metadata.Add("BookingId", request.ReferenceID.ToString());
                metadata.Add("Type", "Space");
            }

            if (string.IsNullOrEmpty(request.ReferenceType))
            {
                return BadRequest("ReferenceType is required (Event or Space)");
            }
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100),
                Currency = "eur",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = metadata
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return Ok(new { clientSecret = intent.ClientSecret });
        }
    }

}