using System;
using System.Data;
using System.Text;
using CoworkingSpace.DAL;
using System.Threading.Tasks;
using CoworkingSpace.Models;

namespace CoworkingSpace.BLL
{
    public class clsUserRoles
    {



        public int UserId { get; set; }
        public int RoleId { get; set; }
        public clsUserRoles()
        {
            this.UserId = -1;
            this.RoleId = -1;

        }

        private clsUserRoles(userRoleModel model)
        {

            this.UserId = model.UserId;
            this.RoleId = model.RoleId;

        }

        public static Task<bool> Delete(int UserId)
        {
            // Call DataAccess Layer
            return clsUserRolesData.DeleteUserRoles(UserId);
        }

        public static async Task<List<userRoleModel>> GetAllUserRoles()
        {
            return await clsUserRolesData.GetAllUserRoles();


        }

        public static  async Task<bool> AddUserRole(userRoleModel roleModel)
        {
            if (roleModel == null || roleModel.UserId <= 0 || roleModel.RoleId <= 0)
                return false;

            return await clsUserRolesData.AddUserRole(roleModel);
        }


        public static async Task<string> GetRoleNameByUserId(int userId)
        {
            if (userId <= 0)
                return null;
            return await clsUserRolesData.GetRoleNameByUserId(userId);
        }

    }
 }