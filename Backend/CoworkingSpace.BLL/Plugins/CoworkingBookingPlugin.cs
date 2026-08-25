using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL.Plugins
{
    public class CoworkingBookingPlugin
    {
        [KernelFunction, Description("Gets the list of available coworking spaces for booking.")]
        public async Task<string> GetAllCoworkingSpacesAsync()
        {
            var  spaces=await clsWorkspaceSpaces.GetAllWorkspaceSpaces();
            var availableSpaces = spaces.Where(s => s.IsAvailable).ToList();

            if (!availableSpaces.Any())
            {
                return "There are no available coworking spaces at the moment.";
            }

           
            var spaceDetails = string.Join("\n", availableSpaces.Select(s => $"- {s.SpaceType}: ${s.PricePerHour} per hour"));

            return $"Here are the available coworking spaces:\n{spaceDetails}\nYou can view details and book your space directly from the Coworking Spaces page.";
        }
        

        [KernelFunction, Description("Gets available coworking spaces filtered by a specific type (e.g., Meeting Room, Private Office, Dedicated Desk, Hot Desk")]
        public async Task<string> GetSpacesByTypeAsync([Description("The type of the workspace like Meeting Room, Private Office")] string spaceType)
        {
            var spaces = await clsWorkspaceSpaces.GetAllWorkspaceSpaces();

            var filtered = spaces.Where(s => s.IsAvailable && s.SpaceType.Contains(spaceType, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!filtered.Any())
            {
                return $"Sorry,no available spaces found for type :{spaceType}";
            }

            return $"Here are the available spaces for '{spaceType}:\n" +
                string.Join("\n", filtered.Select(s => $"- {s.SpaceType} :${s.PricePerHour} per hour"));
        }


        [KernelFunction,Description("Chacks if a spacific coworking space is available for booking.")]
        public async Task<string> CheckSpaceAvailabilityAsync([Description("The name or type of the space")] string spaceName)
        {
            var spaces= await clsWorkspaceSpaces.GetAllWorkspaceSpaces();
            var space = spaces?.FirstOrDefault(s=>s.SpaceType.Contains(spaceName,StringComparison.OrdinalIgnoreCase));

            if(space == null)
            {
                return $"We couldn't find a space named '{spaceName}'.";
            }

            return space.IsAvailable?
                $"Yes! The '{space.SpaceType}' is currently available at ${space.PricePerHour} per hour."
                : $"Unfortunately, the '{space.SpaceType}' is currently booked/unavailable.";
        }


    }


    
}
