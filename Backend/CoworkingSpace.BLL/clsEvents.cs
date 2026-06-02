using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL
{
    public class clsEvents
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;



        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public decimal TicketPrice { get; set; }
        public int MaxAttendees { get; set; }
        public int AvailableSeats { get; set; }
        public clsEvents()
        {
            this.Id = -1;
            this.Title = "";
            this.Description = "";
            this.EventDate = DateTime.MinValue;
            this.TicketPrice = 0.0m;
            this.MaxAttendees = -1;
            this.AvailableSeats = -1;
            this.Mode = enMode.addNew;
        }

        private clsEvents(int Id, string Title, string Description, DateTime EventDate, decimal TicketPrice, int MaxAttendees, int AvailableSeats)
        {
            this.Id = Id;
            this.Title = Title;
            this.Description = Description;
            this.EventDate = EventDate;
            this.TicketPrice = TicketPrice;
            this.MaxAttendees = MaxAttendees;
            this.AvailableSeats = AvailableSeats;

            this.Mode = enMode.update;
        }

        private async Task<bool> _AddNewEvents()
        {
            eventModel model = new eventModel
            {
                Title = this.Title,
                Description = this.Description,
                EventDate = this.EventDate,
                TicketPrice = this.TicketPrice,
                MaxAttendees = this.MaxAttendees,
                AvailableSeats = this.AvailableSeats
            };



            // Call DataAccess Layer
            this.Id = (int)await clsEventsData.AddNewEvents(model);
            return (this.Id != -1);
        }

        public static Task<bool> Delete(int Id)
        {
            // Call DataAccess Layer
            return clsEventsData.DeleteEvents(Id);
        }

        public static clsEvents Find(int Id)
        {
            // Call DataAccess Layer
            eventModel model = new eventModel();

            bool IsFound = clsEventsData.FindByID(Id,model );
            if (IsFound)
                return new clsEvents(Id, model. Title, model. Description, model. EventDate, model. TicketPrice, model. MaxAttendees, model. AvailableSeats);
            return null;
        }

        //public static clsEvents FindByName(string Title)
        //{
        //    // Call DataAccess Layer
        //    int Id = -1;
        //    string Description = "";
        //    DateTime EventDate = DateTime.MinValue;
        //    decimal TicketPrice = 0.0m;
        //    int MaxAttendees = -1;
        //    int AvailableSeats = -1;

        //    bool IsFound = clsEventsData.FindByName(ref Id, Title, ref Description, ref EventDate, ref TicketPrice, ref MaxAttendees, ref AvailableSeats);
        //    if (IsFound)
        //        return new clsEvents(Id, Title, Description, EventDate, TicketPrice, MaxAttendees, AvailableSeats);
        //    else
        //        return null;
        //}

        public static async Task<List<eventModel>> GetAllEvents()
        {
            return await clsEventsData.GetAllEvents();
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    Mode = enMode.update;
                    return await _AddNewEvents();
                case enMode.update:
                    return await _UpdateEvents();
            }
            return false;
        }

        private async Task<bool> _UpdateEvents()
        {
            eventModel model = new eventModel
            {
                Id = this.Id,
                Title = this.Title,
                Description = this.Description,
                EventDate = this.EventDate,
                TicketPrice = this.TicketPrice,
                MaxAttendees = this.MaxAttendees,
                AvailableSeats = this.AvailableSeats
            };
            // Call DataAccess Layer
            return await clsEventsData.UpdateEvents(model) ?? false;
        }

    }
}
