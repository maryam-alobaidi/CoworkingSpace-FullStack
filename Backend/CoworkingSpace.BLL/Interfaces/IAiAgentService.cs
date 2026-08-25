using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL.Interfaces
{
    public interface IAiAgentService
    {
        Task<string> GetResponseAsync(string userInput);
    }
}
