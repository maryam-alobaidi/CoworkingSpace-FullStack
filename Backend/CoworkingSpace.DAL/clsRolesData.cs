using CoworkingSpace.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.DAL
{
    public static class clsRolesData
    {

        public static async Task<int?> AddNewRoles(roleModel model)
        {
          
            using (SqlCommand command = new SqlCommand("Sp_AddNewRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Name", model.Name);

                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;

                return await clsPrimaryFunctions.AddAsync(command,"@NewAddClass");
            }
        }

        public static async Task<bool> DeleteRoles(int Id)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", Id);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<bool?> UpdateRoles(roleModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_UpdateRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", model. Id);
                command.Parameters.AddWithValue("@Name", model. Name);

                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }

        public static async Task<List<roleModel>> GetAllRoles()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<roleModel> roles = new List<roleModel>();
                    while (reader.Read())
                    {
                        roleModel role = new roleModel
                        {
                            Id = (int)reader["Id"],
                            Name = (string)reader["Name"]
                        };
                        roles.Add(role);
                    }
                    return roles;
                }
            }
        }

        public static bool FindByID(int Id, roleModel model)
        {
            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetRolesByID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", Id);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isFound = true;
                                model.Id = Id;
                                model.Name = (string)reader["Name"];

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

    }
}
