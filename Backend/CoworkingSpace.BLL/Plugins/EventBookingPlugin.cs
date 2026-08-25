using CoworkingSpace.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL.Plugins
{
    //Read & Guide
    public class EventBookingPlugin
    {

        [KernelFunction, Description("Gets the list of available events for booking.")]
        public async Task<string> GetAllEventsAsync()
        {
            var events = await clsEvents.GetAllEvents();

            if (events == null || !events.Any())
            {
                return "There are no upcoming events available at the moment.";
            }

            DateTime today = DateTime.Today;

            var upcomingEvents = events.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList();

            if (!upcomingEvents.Any())
            {
                return "There are no upcoming events scheduled from today onwards.";
            }

            var eventsList = string.Join("\n", upcomingEvents.Select(e => $"- Event: {e.Title}, Date: {e.EventDate.ToShortDateString()}, Price: {e.TicketPrice}"));

            return $"Here are the available events:\n{eventsList}\nYou can view details and book your ticket directly from the 'Events' page on our website: /events";
        }

        [KernelFunction, Description("Gets the list of past events for review.")]
        public async Task<string> GetAllPastEventsAsync()
        {
            var events = await clsEvents.GetAllEvents();

            if (events == null || !events.Any())
            {
                return "There are no events available at the moment.";
            }

            DateTime today = DateTime.Today;


            var pastEvents = events.Where(e => e.EventDate < today).OrderBy(e => e.EventDate).ToList();

            if (!pastEvents.Any())
            {
                return "There are no past events for review.";
            }

            var eventsList = string.Join("\n", pastEvents.Select(e => $"- Event: {e.Title}, Date: {e.EventDate.ToShortDateString()}, Price: {e.TicketPrice}"));

            return $"Here are the past events:\n{eventsList}\nYou can view all details directly from the Events page: /events";
        }


        [KernelFunction, Description("Gets the events details by title.")]
        public async Task<string> GetEventsByTitleAsync([Description("The title (name) of event.")] string title)
        {
            var events = await clsEvents.GetAllEvents();

            if (events == null || !events.Any())
            {
                return "There are no events available at the moment.";
            }

            DateTime today = DateTime.Today;

            var targetEvent = events.FirstOrDefault(e => e.EventDate >= today &&
                e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (targetEvent == null)
            {
                return $"There are no upcoming events with this title '{title}'.";
            }

            return $"Here are the event details:\n- Title: {targetEvent.Title}\n- Date: {targetEvent.EventDate.ToShortDateString()}\n- Price: ${targetEvent.TicketPrice}\nYou can view details directly from the Events page: /event-book/{targetEvent.Id}";
        }
    }

}
