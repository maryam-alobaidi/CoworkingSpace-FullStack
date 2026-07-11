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
    public static class clsEventTicketsData
    {
        public static async Task<int?> AddNewEventTickets(eventTicketModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddNewEventTickets"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@EventId", model.EventId);
                command.Parameters.AddWithValue("@UserId", model.UserId);
                command.Parameters.AddWithValue("@TicketCode", model.TicketCode);
                command.Parameters.AddWithValue("@PurchaseDate", model.PurchaseDate);

                // --- الخصائص الجديدة ---
                command.Parameters.AddWithValue("@PaymentStatus", model.PaymentStatus ?? "Pending");
                command.Parameters.AddWithValue("@TransactionId", (object)model.TransactionId ?? DBNull.Value);

                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;

                return await clsPrimaryFunctions.AddAsync(command, "@NewAddClass");
            }
        }

        public static async Task<bool> UpdateEventTickets(eventTicketModel model)
        {
            if (model.Id == null || model.Id <= 0) return false;

            using (SqlCommand command = new SqlCommand("Sp_UpdateEventTickets"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", model.Id);
                command.Parameters.AddWithValue("@EventId", model.EventId);
                command.Parameters.AddWithValue("@UserId", model.UserId);
                command.Parameters.AddWithValue("@TicketCode", model.TicketCode);
                command.Parameters.AddWithValue("@PurchaseDate", model.PurchaseDate);

                // --- الخصائص الجديدة ---
                command.Parameters.AddWithValue("@PaymentStatus", model.PaymentStatus);
                command.Parameters.AddWithValue("@TransactionId", (object)model.TransactionId ?? DBNull.Value);

                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }

        public static async Task<List<eventTicketModel>> GetAllEventTickets()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllEventTickets"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<eventTicketModel> eventTicketsList = new List<eventTicketModel>();
                    while (reader.Read())
                    {
                        eventTicketsList.Add(new eventTicketModel
                        {
                            Id = (int)reader["Id"],
                            EventId = (int)reader["EventId"],
                            UserId = (int)reader["UserId"],
                            TicketCode = (string)reader["TicketCode"],
                            PurchaseDate = (DateTime)reader["PurchaseDate"],

                            PaymentStatus = reader["PaymentStatus"] != DBNull.Value ? (string)reader["PaymentStatus"] : null,
                            TransactionId = reader["TransactionId"] != DBNull.Value ? (string)reader["TransactionId"] : null
                        });
                    }
                    return eventTicketsList;
                }
            }
        }

        public static async Task<eventTicketModel> FindByID(int Id)
        {
            eventTicketModel? model = null;
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetEventTicketsByID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", Id);
                    try
                    {
                        // 1️⃣ فتح الاتصال بشكل Async
                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                model = new eventTicketModel();
                                model.Id = (int)reader["Id"];
                                model.EventId = (int)reader["EventId"];
                                model.UserId = (int)reader["UserId"];

                                // معالجة النصوص بشكل آمن لحمايتها من الـ Null
                                model.TicketCode = reader["TicketCode"] != DBNull.Value ? (string)reader["TicketCode"] : null;
                                model.PurchaseDate = reader["PurchaseDate"] != DBNull.Value ? (DateTime)reader["PurchaseDate"] : DateTime.Now;

                                model.PaymentStatus = reader["PaymentStatus"] != DBNull.Value ? (string)reader["PaymentStatus"] : null;
                                model.TransactionId = reader["TransactionId"] != DBNull.Value ? (string)reader["TransactionId"] : null;
                                model.TotalPrice = reader["TotalPrice"] != DBNull.Value ? (decimal)reader["TotalPrice"] : 0;
                                model.EventTitle = reader["EventTitle"] != DBNull.Value ? (string)reader["EventTitle"] : "Event Ticket";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        clsPrimaryFunctions.EntireInfoToEventLoge(ex.Message);
                    }
                }
            }
            return model;
        }

        public static async Task<bool> DeleteEventTickets(int Id)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteEventTickets"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", Id);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<List<eventTicketModel>> GetTicketsByUserId(int userId)
        {
            using (SqlCommand command = new SqlCommand("Sp_GetEventTicketsByUserId"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<eventTicketModel> userTicketsList = new List<eventTicketModel>();

                    while (reader.Read())
                    {
                        userTicketsList.Add(new eventTicketModel
                        {
                            Id = (int)reader["Id"],
                            EventId = (int)reader["EventId"],
                            UserId = (int)reader["UserId"],
                            TicketCode = (string)reader["TicketCode"],
                            PurchaseDate = (DateTime)reader["PurchaseDate"],


                            PaymentStatus = reader["PaymentStatus"] != DBNull.Value ? (string)reader["PaymentStatus"] : null,
                            TransactionId = reader["TransactionId"] != DBNull.Value ? (string)reader["TransactionId"] : null,

                            // الحقول الجديدة القادمة من الـ INNER JOIN مع جدول الفعاليات
                            EventTitle = reader["EventTitle"] != DBNull.Value ? (string)reader["EventTitle"] : null,
                            TotalPrice = reader["TicketPrice"] != DBNull.Value ? Convert.ToDecimal(reader["TicketPrice"]) : null
                        });
                    }
                    return userTicketsList;
                }
            }
        }

        public static async Task<List<RecentEventTicket>> GetRecentEventTicket()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetRecentEventTickets"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<RecentEventTicket> recentTicketsList = new List<RecentEventTicket>();
                    while (reader.Read())
                    {
                        recentTicketsList.Add(new RecentEventTicket
                        {
                            TicketId= (int)reader["TicketId"],
                            UserName = reader["UserName"] != DBNull.Value ? (string)reader["UserName"] : null,
                            EventName = reader["EventName"] != DBNull.Value ? (string)reader["EventName"] : null,
                            PurchaseDate = reader["PurchaseDate"] != DBNull.Value ? (DateTime)reader["PurchaseDate"] : DateTime.Now,
                            Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                            Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : null

                        });
                    }
                    return recentTicketsList;
                }
            }
        }

    }
}