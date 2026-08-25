using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.Models
{
    public class AiChatResponseModel
    {

        //the response from the AI chat model
        public string Message { get; set; }

        public string ActionType { get; set; }= "none"; // default value is "none"

        public string TargetUrl { get; set; }
    }
}
