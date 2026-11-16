using Microsoft.AspNetCore.Mvc;

namespace CoworkingSpace.API.Infrastructure
{
    public static class ControllerErrorExtensions
    {
        /// <summary>
        /// Logs the exception server-side and returns a 500 with a generic message,
        /// so internal details (SQL errors, stack info) never reach the client.
        /// </summary>
        public static ObjectResult ServerError(this ControllerBase controller, Exception ex, string message)
        {
            var logger = controller.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(controller.GetType());

            logger.LogError(ex, "{Message} ({Method} {Path})", message,
                controller.HttpContext.Request.Method, controller.HttpContext.Request.Path);

            return controller.StatusCode(StatusCodes.Status500InternalServerError, new { message });
        }
    }
}
