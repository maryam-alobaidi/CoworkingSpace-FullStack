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
    public static class clsWorkspaceSpacesData
    {

        public static async Task<int?> AddNewWorkspaceSpaces(workspaceSpaceModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddNewWorkspaceSpaces"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Title", model.Title);
                command.Parameters.AddWithValue("@Description", model.Description);
                command.Parameters.AddWithValue("@SpaceType", model.SpaceType);
                command.Parameters.AddWithValue("@PricePerHour", model.PricePerHour);
                command.Parameters.AddWithValue("@PricePerDay", model.PricePerDay);
                command.Parameters.AddWithValue("@Capacity", model.Capacity);
                command.Parameters.AddWithValue("@IsAvailable", model.IsAvailable);

                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;

                return await clsPrimaryFunctions.AddAsync(command, "@NewAddClass");
            }
        }

        public static async Task<bool> DeleteWorkspaceSpaces(int Id)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteWorkspaceSpaces"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", Id);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<List<workspaceSpaceModel>> GetAllWorkspaceSpaces()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllWorkspaceSpaces"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    var list = new List<workspaceSpaceModel>();
                    while (reader.Read())
                    {
                        list.Add(new workspaceSpaceModel
                        {
                            Id = (int)reader["Id"],
                            Title = (string)reader["Title"],
                            Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null,
                            SpaceType = (string)reader["SpaceType"],
                            PricePerHour = (decimal)reader["PricePerHour"],
                            PricePerDay = (decimal)reader["PricePerDay"],
                            Capacity = (int)reader["Capacity"],
                            IsAvailable = (bool)reader["IsAvailable"]
                        });
                    }
                    return list;
                }
            }
        }

        // في ملف clsWorkspaceSpacesData.cs
        public static async Task<bool> FindByID(int Id, workspaceSpaceModel model) // أضيفي async Task
        {
            bool isFound = false;
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetWorkspaceSpacesByID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", Id);
                    try
                    {
                        await connection.OpenAsync(); 
                        using (SqlDataReader reader = await command.ExecuteReaderAsync()) 
                        {
                            if (await reader.ReadAsync()) 
                            {
                                isFound = true;
                                
                                model.Id = (int)reader["Id"];
                                model.Title = (string)reader["Title"];
                                model.Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null;
                                model.SpaceType = (string)reader["SpaceType"];
                                model.PricePerHour = (decimal)reader["PricePerHour"];
                                model.PricePerDay = (decimal)reader["PricePerDay"];
                                model.Capacity = (int)reader["Capacity"];
                                model.IsAvailable = (bool)reader["IsAvailable"];
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

        public static async Task<bool?> UpdateWorkspaceSpaces( workspaceSpaceModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_UpdateWorkspaceSpaces"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id",model. Id);
                command.Parameters.AddWithValue("@Title", model. Title);
                command.Parameters.AddWithValue("@Description", model. Description);
                command.Parameters.AddWithValue("@SpaceType", model. SpaceType);
                command.Parameters.AddWithValue("@PricePerHour", model.PricePerHour);
                command.Parameters.AddWithValue("@PricePerDay", model. PricePerDay);
                command.Parameters.AddWithValue("@Capacity", model. Capacity);
                command.Parameters.AddWithValue("@IsAvailable", model. IsAvailable);

                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }

    }

}
