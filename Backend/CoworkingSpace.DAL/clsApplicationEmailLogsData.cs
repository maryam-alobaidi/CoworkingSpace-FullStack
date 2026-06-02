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
    public static class clsApplicationEmailLogsData
    {

        public static async Task<bool> DeleteApplicationEmailLogs(int LogID)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteApplicationEmailLogs"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@LogID", LogID);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<List<applicationEmailLogsModel>> GetAllApplicationEmailLogs()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllApplicationEmailLogs"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<applicationEmailLogsModel> logs = new List<applicationEmailLogsModel>();
                    while (await reader.ReadAsync())
                    {
                        logs.Add(new applicationEmailLogsModel
                        {
                            LogID = (int)reader["LogID"],
                            ReferenceID = (int)reader["ReferenceID"],
                            LogType = (string)reader["LogType"],
                            RecipientEmail = (string)reader["RecipientEmail"],
                            Subject = (string)reader["Subject"],
                            Body = (string)reader["Body"],
                            SentDate = reader["SentDate"] != DBNull.Value ? (DateTime)reader["SentDate"] : default(DateTime),
                            Status = (string)reader["Status"],
                            ErrorMessage = reader["ErrorMessage"] != DBNull.Value ? (string)reader["ErrorMessage"] : null
                        });
                    }
                    return logs;
                }
            }
        }

        public static bool FindByID(int LogID, applicationEmailLogsModel model)
        {
            bool isFound = false;
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetApplicationEmailLogsByID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@LogID", LogID);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isFound = true;
                                model.ReferenceID = (int)reader["ReferenceID"];
                                model.LogType = (string)reader["LogType"];
                                model.RecipientEmail = (string)reader["RecipientEmail"];
                                model.Subject = (string)reader["Subject"];
                                model.Body = (string)reader["Body"];
                                model.SentDate = reader["SentDate"] != DBNull.Value ? (DateTime)reader["SentDate"] : default(DateTime);
                                model.Status = (string)reader["Status"];
                                model. ErrorMessage = reader["ErrorMessage"] != DBNull.Value ? (string)reader["ErrorMessage"] : null;

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

        public static async Task<bool> UpdateApplicationEmailLogs(applicationEmailLogsModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_UpdateApplicationEmailLogs"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@LogID", model.LogID);
                command.Parameters.AddWithValue("@ReferenceID", model.ReferenceID);
                command.Parameters.AddWithValue("@LogType", model.LogType);
                command.Parameters.AddWithValue("@RecipientEmail", model.RecipientEmail);
                command.Parameters.AddWithValue("@Subject", model.Subject);
                command.Parameters.AddWithValue("@Body", model.Body);
                command.Parameters.AddWithValue("@SentDate", model.SentDate);
                command.Parameters.AddWithValue("@Status", model.Status);
                command.Parameters.AddWithValue("@ErrorMessage", model.ErrorMessage ?? (object)DBNull.Value);
                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }

        public static async Task<int?> AddNewApplicationEmailLogs(applicationEmailLogsModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddNewApplicationEmailLogs"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ReferenceID", model.ReferenceID);
                command.Parameters.AddWithValue("@LogType", model.LogType);
                command.Parameters.AddWithValue("@RecipientEmail", model.RecipientEmail);
                command.Parameters.AddWithValue("@Subject", model.Subject);
                command.Parameters.AddWithValue("@Body", model.Body);
                command.Parameters.AddWithValue("@SentDate", model.SentDate);
                command.Parameters.AddWithValue("@Status", model.Status);
                command.Parameters.AddWithValue("@ErrorMessage", model.ErrorMessage ?? (object)DBNull.Value);

                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;

                return await clsPrimaryFunctions.AddAsync(command, "@NewAddClass");
            }
        }
    }

}
