using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using System;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL
{
    public class clsWorkspaceSpaces
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;



        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SpaceType { get; set; }
        public decimal PricePerHour { get; set; }
        public decimal PricePerDay { get; set; }
        public int Capacity { get; set; }
        public bool IsAvailable { get; set; }
        public clsWorkspaceSpaces()
        {
            this.Id = -1;
            this.Title = "";
            this.Description = "";
            this.SpaceType = "";
            this.PricePerHour = 0.0m;
            this.PricePerDay = 0.0m;
            this.Capacity = -1;
            this.IsAvailable = false;
            this.Mode = enMode.addNew;
        }


        public clsWorkspaceSpaces(workspaceSpaceModel model)
        {
            Id =(int) model.Id;
            Title = model.Title;
            Description = model.Description;
            SpaceType = model.SpaceType;
            PricePerHour = model.PricePerHour;
            PricePerDay = model.PricePerDay;
            Capacity = model.Capacity;
            IsAvailable = model.IsAvailable;
            this.Mode = enMode.update;
        }

        private async Task<bool> _AddNewWorkspaceSpaces()
        {

            workspaceSpaceModel model = new workspaceSpaceModel
            {
                Title = this.Title,
                Description = this.Description,
                SpaceType = this.SpaceType,
                PricePerHour = this.PricePerHour,
                PricePerDay = this.PricePerDay,
                Capacity = this.Capacity,
                IsAvailable = this.IsAvailable
            };
            // Call DataAccess Layer
            this.Id = (int)await clsWorkspaceSpacesData.AddNewWorkspaceSpaces(model);
            return (this.Id != -1);
        }

        public static Task<bool> Delete(int Id)
        {
            // Call DataAccess Layer
            return clsWorkspaceSpacesData.DeleteWorkspaceSpaces(Id);
        }

        public static async Task<clsWorkspaceSpaces> Find(int Id)
        {
            // Call DataAccess Layer
            workspaceSpaceModel model = new workspaceSpaceModel();

            bool IsFound = await clsWorkspaceSpacesData.FindByID(Id, model);
            if (IsFound)
                return new clsWorkspaceSpaces(model);
            return null;
        }

      

        public static async Task<List<workspaceSpaceModel>> GetAllWorkspaceSpaces()
        {
            return await clsWorkspaceSpacesData.GetAllWorkspaceSpaces();
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    Mode = enMode.update;
                    return await _AddNewWorkspaceSpaces();
                case enMode.update:
                    return await _UpdateWorkspaceSpaces();
            }
            return false;
        }

        private async Task<bool> _UpdateWorkspaceSpaces()
        {
            workspaceSpaceModel model = new workspaceSpaceModel
            {
                Id = this.Id,
                Title = this.Title,
                Description = this.Description,
                SpaceType = this.SpaceType,
                PricePerHour = this.PricePerHour,
                PricePerDay = this.PricePerDay,
                Capacity = this.Capacity,
                IsAvailable = this.IsAvailable
            };

            // Call DataAccess Layer
            return await clsWorkspaceSpacesData.UpdateWorkspaceSpaces(model) ?? false;
        }

    }
}
