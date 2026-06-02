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
    public class clsRoles
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;



        public int Id { get; set; }
        public string Name { get; set; }
        public clsRoles()
        {
            this.Id = -1;
            this.Name = "";
            this.Mode = enMode.addNew;
        }

        private clsRoles(int Id, string Name)
        {
            this.Id = Id;
            this.Name = Name;

            this.Mode = enMode.update;
        }

        private async Task<bool> _AddNewRoles()
        {
            roleModel model = new roleModel
            {
                Name = this.Name
            };
            // Call DataAccess Layer
            this.Id = (int)await clsRolesData.AddNewRoles(model);
            return (this.Id != -1);
        }

        public static Task<bool> Delete(int Id)
        {
            // Call DataAccess Layer
            return clsRolesData.DeleteRoles(Id);
        }

        public static clsRoles Find(int Id)
        {
            // Call DataAccess Layer
            roleModel model = new roleModel();

            bool IsFound = clsRolesData.FindByID(Id,model);
            if (IsFound)
            { return new clsRoles(Id, model.Name); }
            else { return null; }
        }

        //public static clsRoles FindByName(string Name)
        //{
        //    // Call DataAccess Layer
        //    int Id = -1;

        //    bool IsFound = clsRolesData.FindByName(ref Id, Name,);
        //    if (IsFound)
        //        return new clsRoles(Id, Name);
        //    else
        //        return null;
        //}

        public static async Task<List<roleModel>> GetAllRoles()
        {
            return await clsRolesData.GetAllRoles();
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    Mode = enMode.update;
                    return await _AddNewRoles();
                case enMode.update:
                    return await _UpdateRoles();
            }
            return false;
        }

        private async Task<bool> _UpdateRoles()
        {
            roleModel model = new roleModel
            {
                Id= this.Id,
                Name = this.Name
            };
            // Call DataAccess Layer
            return await clsRolesData.UpdateRoles(model) ?? false;
        }

    }

}
