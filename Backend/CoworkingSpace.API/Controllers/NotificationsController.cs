using CoworkingSpace.API.Security;
using CoworkingSpace.BLL;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        // get notifications for specific user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetNotifications(int userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var notifications = await clsNotifications.GetAllNotificationsByUserID(userId);

            if (notifications == null || notifications.Count == 0)
                return Ok(new List<clsNotifications>());

            return Ok(notifications);

        }

        // update notification as read
        [HttpPut("mark-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            bool isUpdated = await clsNotifications.MarkNotificationAsRead(notificationId);

            if (!isUpdated)
                return StatusCode(500, "Failed to update the notification.");

           return Ok("Notification marked as read successfully.");
        }


        // add new notification
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("add")]
        public async Task<IActionResult> AddNotification([FromBody] notificationsModel model)
        {
            // Create an instance of clsNotifications and populate its properties from the model
            var notification = new clsNotifications
            {
                UserID = model.UserID,
                Title = model.Title,
                Message = model.Message,
                NotificationType = model.NotificationType,
                TargetURL = model.TargetURL,
                IsRead = model.IsRead,
                CreatedAt = model.CreatedAt,
                ReadAt = model.ReadAt,
                Mode = clsNotifications.enMode.addNew
            };

            bool isAdded = await notification.AddNewNotifications();

            if (!isAdded)
               return
                    StatusCode(500, "Failed to add the notification.");

         return Ok(new { Message = "Notification added successfully.", NotificationID = notification.NotificationID });
        }
    }
}
