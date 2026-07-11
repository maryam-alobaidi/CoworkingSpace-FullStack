using System;
using System.Data;
using System.Text;
using CoworkingSpace.DAL;
using System.Threading.Tasks;
using CoworkingSpace.Models;
using System.Collections.Generic;

namespace CoworkingSpace.BLL
{
    public class clsUsers
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;

        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSuspended { get; set; } = false; 

        public clsUsers()
        {
            this.Id = -1;
            this.FullName = "";
            this.Email = "";
            this.PasswordHash = "";
            this.PasswordSalt = "";
            this.PhoneNumber = "";
            this.IsEmailConfirmed = true;
            this.CreatedAt = DateTime.Now;
            this.IsSuspended = false;
            this.Mode = enMode.addNew;
        }

        private clsUsers(userModel model)
        {
            this.Id = (int)model.Id;
            this.FullName = model.FullName;
            this.Email = model.Email;
            this.PasswordHash = model.PasswordHash;
            this.PasswordSalt = model.PasswordSalt;
            this.PhoneNumber = model.PhoneNumber;
            this.IsEmailConfirmed = model.IsEmailConfirmed;
            this.CreatedAt = model.CreatedAt;
            this.IsSuspended = model.IsSuspended;
            this.Mode = enMode.update;
        }

        private async Task<bool> _AddNewUsers()
        {
            userModel model = new userModel
            {
                FullName = this.FullName,
                Email = this.Email,
                PasswordHash = this.PasswordHash,
                PasswordSalt = this.PasswordSalt,
                PhoneNumber = this.PhoneNumber,
                IsEmailConfirmed = this.IsEmailConfirmed,
                CreatedAt = this.CreatedAt,
                IsSuspended = this.IsSuspended 
            };

            this.Id = (int)await clsUsersData.AddNewUsers(model);
            return (this.Id != -1);
        }

        private async Task<bool> _UpdateUsers()
        {
            userModel model = new userModel
            {
                Id = this.Id,
                FullName = this.FullName,
                Email = this.Email,
                PasswordHash = this.PasswordHash,
                PasswordSalt = this.PasswordSalt,
                PhoneNumber = this.PhoneNumber,
                IsEmailConfirmed = this.IsEmailConfirmed,
                CreatedAt = this.CreatedAt,
                IsSuspended = this.IsSuspended 
            };

            return await clsUsersData.UpdateUsers(model) ?? false;
        }

       
        public static async Task<bool> ToggleSuspend(int Id)
        {
            clsUsers user = clsUsers.Find(Id);
            if (user == null) return false;

          
            user.IsSuspended = !user.IsSuspended;

            return await user.Save(); 
        }

        public static Task<bool> Delete(int Id)
        {
            return clsUsersData.DeleteUsers(Id);
        }

        public static clsUsers Find(int Id)
        {
            userModel model = new userModel();
            model.Id = Id;
            bool IsFound = clsUsersData.FindByID(model);
            if (IsFound)
                return new clsUsers(model);
            return null;
        }

        public static async Task<List<userModel>> GetAllUsers()
        {
            return await clsUsersData.GetAllUsers();
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    Mode = enMode.update;
                    return await _AddNewUsers();
                case enMode.update:
                    return await _UpdateUsers();
            }
            return false;
        }

        public static async Task<clsUsers> FindByEmail(string email)
        {
            userModel model = await clsUsersData.FindByEmail(email);
            if (model != null)
                return new clsUsers(model);
            return null;
        }

        public static async Task<int?> getTotalMembersCount()
        {
            return await clsUsersData.getTotalMembersCount();
        }

        public static async Task<List<UserWithRoleDto>> getUsersWhitRole()
        {
            return await clsUsersData.getUsersWhitRole();
        }
    }
}