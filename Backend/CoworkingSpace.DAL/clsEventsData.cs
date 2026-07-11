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
    public static class clsEventsData
    {

        public static async Task<int?> AddNewEvents(eventModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddNewEvents"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Title", model.Title);
                command.Parameters.AddWithValue("@Description", model.Description);
                command.Parameters.AddWithValue("@EventDate", model.EventDate);
                command.Parameters.AddWithValue("@TicketPrice", model.TicketPrice);
                command.Parameters.AddWithValue("@MaxAttendees", model. MaxAttendees);
                command.Parameters.AddWithValue("@AvailableSeats", model. AvailableSeats);

                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;

                return await clsPrimaryFunctions.AddAsync(command, "@NewAddClass");
            }
        }

        public static async Task<bool> DeleteEvents(int Id)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteEvents"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", Id);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<bool?> UpdateEvents(eventModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_UpdateEvents"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", model.Id);
                command.Parameters.AddWithValue("@Title", model.Title);
                command.Parameters.AddWithValue("@Description", model.Description);
                command.Parameters.AddWithValue("@EventDate", model. EventDate);
                command.Parameters.AddWithValue("@TicketPrice", model.TicketPrice);
                command.Parameters.AddWithValue("@MaxAttendees", model. MaxAttendees);
                command.Parameters.AddWithValue("@AvailableSeats", model.AvailableSeats);


                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }

        public static async Task<List<eventModel>> GetAllEvents()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllEvents"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<eventModel> events = new List<eventModel>();
                    while (await reader.ReadAsync())
                    {
                        events.Add(new eventModel
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            EventDate = reader.GetDateTime(3),
                            TicketPrice = reader.GetDecimal(4),
                            MaxAttendees = reader.GetInt32(5),
                            AvailableSeats = reader.GetInt32(6)
                        });
                    }
                    return events;
                }
            }
        }

        public static bool FindByID(int Id, eventModel model)
        {
            bool isFound = false;
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetEventsByID", connection))
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
                                model.Id= (int)reader["Id"];
                                model. Title = (string)reader["Title"];
                                model.Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null;
                                model. EventDate = (DateTime)reader["EventDate"];
                                model.TicketPrice = (decimal)reader["TicketPrice"];
                                model. MaxAttendees = (int)reader["MaxAttendees"];
                                model.AvailableSeats = (int)reader["AvailableSeats"];

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


        public static async Task<int?> getUpcomingEventsCount()
        {

            using (SqlCommand command = new SqlCommand("sp_GetUpcomingEventsCount"))
            {
                command.CommandType = CommandType.StoredProcedure;
                return await clsPrimaryFunctions.GetScalarAsync(command);
            }
        }

    }
}
