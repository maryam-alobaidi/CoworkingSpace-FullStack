using CoworkingSpace.API.Infrastructure;
using CoworkingSpace.API.Security;
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



        private const int MaxMessageLength = 2000;

        [HttpPost("send")]
        public async Task<ActionResult<AiChatResponseModel>> SendMessage([FromBody] AiChatRequestModel request)
        {
            string userInput = request?.Message?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(userInput))
            {
                return BadRequest("User input cannot be empty.");
            }
            if (userInput.Length > MaxMessageLength)
            {
                return BadRequest($"Message is too long (max {MaxMessageLength} characters).");
            }

            // History is keyed by the client's conversation GUID, scoped to the logged-in user when there is one.
            string conversationId = Guid.TryParse(request!.ConversationId, out var cid) ? cid.ToString("N") : Guid.NewGuid().ToString("N");
            string conversationKey = $"{User.GetUserId()?.ToString() ?? "anon"}:{conversationId}";

            string responseMessage;
            try
            {
                responseMessage = await _aiAgentService.GetResponseAsync(conversationKey, userInput);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex, "The assistant is unavailable right now. Please try again later.");
            }



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