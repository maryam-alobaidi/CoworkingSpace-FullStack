using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendBookingConfirmationAsync(clsApplicationEmailLogs emailLog);

        Task<bool> SendEventConfirmationAsync(clsApplicationEmailLogs eventEmailLog);
    }
}
