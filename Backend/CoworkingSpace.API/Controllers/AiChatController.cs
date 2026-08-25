using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiChatController : ControllerBase
    {

        private readonly IAiAgentService _aiAgentService;


        public AiChatController(IAiAgentService aiAgentService)
        {
            _aiAgentService = aiAgentService;
        }



        [HttpPost("send")]
        public async Task<ActionResult<AiChatResponseModel>> SendMessage([FromBody] string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                return BadRequest("User input cannot be empty.");
            }


            var responseMessage = await _aiAgentService.GetResponseAsync(userInput);



            if (responseMessage == null)
            {
                return StatusCode(500, "Error processing the request.");
            }

            string actionType = "none";
            string? targetUrl = null;

            if (userInput.Contains("past event", StringComparison.OrdinalIgnoreCase))
            {
               
                actionType = "none";
                targetUrl = null;
            }

            else if (responseMessage.Contains("/event-book/"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(responseMessage, @"/event-book/\d+");
                if (match.Success)
                {
                    actionType = "Navigate";
                    targetUrl = match.Value; 
                }
            }

            else if(userInput.Contains("event", StringComparison.OrdinalIgnoreCase))
            {
                actionType = "Navigate";
                targetUrl = "/events";
            }

            else if (userInput.Contains("space", StringComparison.OrdinalIgnoreCase) ||
             userInput.Contains("booking", StringComparison.OrdinalIgnoreCase))
            {
                actionType = "Navigate";
                targetUrl = "/workspace";
            }


            var response = new AiChatResponseModel
            {
                Message = responseMessage,
                ActionType = actionType,
                TargetUrl = targetUrl
            };
            return Ok(response);
        }
    }
}