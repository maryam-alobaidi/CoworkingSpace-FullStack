using CoworkingSpace.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.DAL
{

    public static class clsUserRolesData
    {

        public static async Task<bool> DeleteUserRoles(int UserId)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteUserRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", UserId);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<List<userRoleModel>> GetAllUserRoles()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllUserRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<userRoleModel> userRoles = new List<userRoleModel>();
                    while (reader.Read())
                    {
                        userRoleModel userRole = new userRoleModel
                        {
                            UserId = (int)reader["UserId"],
                            RoleId = (int)reader["RoleId"]
                        };
                        userRoles.Add(userRole);
                    }
                    return userRoles;
                }
            }
        }

        public static bool FindByID(int UserId, int RoleId)
        {
            bool isFound = false;
            using (SqlConnection connection = new SqlConnection("Server=.;Database=CoworkSpaceDB;User ID =sa; Password=Haider2016"))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetUserRolesByID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", UserId);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isFound = true;
                                RoleId = (int)reader["RoleId"];

                            }
                            else
                            {
                                isFound = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        isFound = false;
                        clsPrimaryFunctions.EntireInfoToEventLoge(ex.Message);
                    }
                }
            }
            return isFound;
        }

        public static async Task<bool> AddUserRole(userRoleModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddUserRole"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", model.UserId);
                command.Parameters.AddWithValue("@RoleId", model.RoleId);
                return await clsPrimaryFunctions.AddUserRoleAsync(command);
            }
        }
    }
}