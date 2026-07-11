using CoworkingSpace.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.DAL
{
    public static class clsSpaceBookingsData
    {
        public static async Task<int?> AddNewSpaceBookings(spaceBookingsModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddNewSpaceBookings"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", model.UserId);
                command.Parameters.AddWithValue("@SpaceId", model.SpaceId);
                command.Parameters.AddWithValue("@BookingDate", model.BookingDate);
                command.Parameters.AddWithValue("@StartTime", model.StartTime);
                command.Parameters.AddWithValue("@EndTime", model.EndTime);
                command.Parameters.AddWithValue("@TotalPrice", model.TotalPrice);
                command.Parameters.AddWithValue("@BookingStatus", model.BookingStatus);
                command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

                // --- الحقول الجديدة المضافة ---
                command.Parameters.AddWithValue("@PaymentStatus", model.PaymentStatus ?? "Pending");
                command.Parameters.AddWithValue("@TransactionId", (object)model.TransactionId ?? DBNull.Value);

                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;
                return await clsPrimaryFunctions.AddAsync(command, "@NewAddClass");
            }
        }

        public static async Task<bool?> UpdateSpaceBookings(spaceBookingsModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_UpdateSpaceBookings"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", model.Id);
                command.Parameters.AddWithValue("@UserId", model.UserId);
                command.Parameters.AddWithValue("@SpaceId", model.SpaceId);
                command.Parameters.AddWithValue("@BookingDate", model.BookingDate);
                command.Parameters.AddWithValue("@StartTime", model.StartTime);
                command.Parameters.AddWithValue("@EndTime", model.EndTime);
                command.Parameters.AddWithValue("@TotalPrice", model.TotalPrice);
                command.Parameters.AddWithValue("@BookingStatus", model.BookingStatus);
                command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

                // --- الحقول الجديدة المضافة ---
                command.Parameters.AddWithValue("@PaymentStatus", model.PaymentStatus);
                command.Parameters.AddWithValue("@TransactionId", (object)model.TransactionId ?? DBNull.Value);

                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }

        public static async Task<List<spaceBookingsModel>> GetAllSpaceBookings()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllSpaceBookings"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<spaceBookingsModel> spaceBookingsList = new List<spaceBookingsModel>();
                    while (reader.Read())
                    {
                        spaceBookingsList.Add(new spaceBookingsModel
                        {
                            Id = (int)reader["Id"],
                            UserId = (int)reader["UserId"],
                            SpaceId = (int)reader["SpaceId"],
                            BookingDate = (DateTime)reader["BookingDate"],
                            StartTime = reader["StartTime"] != DBNull.Value ? (string)reader["StartTime"].ToString() : null,
                            EndTime = reader["EndTime"] != DBNull.Value ? (string)reader["EndTime"].ToString() : null,
                            TotalPrice = (decimal)reader["TotalPrice"],
                            BookingStatus = (string)reader["BookingStatus"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            // --- قراءة الحقول الجديدة ---
                            PaymentStatus = reader["PaymentStatus"] != DBNull.Value ? (string)reader["PaymentStatus"] : "Pending",
                            TransactionId = reader["TransactionId"] != DBNull.Value ? (string)reader["TransactionId"] : null
                        });
                    }
                    return spaceBookingsList;
                }
            }
        }

        public static bool FindByID(int Id, spaceBookingsModel model)
        {
            bool isFound = false;
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetSpaceBookingsByID", connection))
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
                                model.UserId = (int)reader["UserId"];
                                model.SpaceId = (int)reader["SpaceId"];
                                model.BookingDate = (DateTime)reader["BookingDate"];
                                // التعديل الصحيح لاستلام الوقت وتحويله لنص
                                model.StartTime = reader["StartTime"] != DBNull.Value ? reader["StartTime"].ToString() : null;
                                model.EndTime = reader["EndTime"] != DBNull.Value ? reader["EndTime"].ToString() : null;



                                model.TotalPrice = (decimal)reader["TotalPrice"];
                                model.BookingStatus = (string)reader["BookingStatus"];
                                model.CreatedAt = (DateTime)reader["CreatedAt"];
                                // --- تعبئة الحقول الجديدة ---
                                model.PaymentStatus = reader["PaymentStatus"] != DBNull.Value ? (string)reader["PaymentStatus"] : "Pending";
                                model.TransactionId = reader["TransactionId"] != DBNull.Value ? (string)reader["TransactionId"] : null;
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

        public static async Task<bool> DeleteSpaceBookings(int Id)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteSpaceBookings"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", Id);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<List<string>> GetBookedSlots(int spaceId, DateTime bookingDate)
        {
            using (SqlCommand command = new SqlCommand("Sp_GetBookedSlots"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@SpaceId", spaceId);
                command.Parameters.AddWithValue("@BookingDate", bookingDate);
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<string> bookedSlots = new List<string>();
                    while (reader.Read())
                    {
                        string startTime = reader["StartTime"] != DBNull.Value ? reader["StartTime"].ToString() : null;
                        string endTime = reader["EndTime"] != DBNull.Value ? reader["EndTime"].ToString() : null;
                        if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
                        {
                            bookedSlots.Add($"{startTime} - {endTime}");
                        }
                    }
                    return bookedSlots;
                }
            }
        }

        public static async Task<List<spaceBookingsModel>> getUserBooking(int id)
        {
            using (SqlCommand command = new SqlCommand("Sp_GetBookedUser"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", id);

                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<spaceBookingsModel> spaceBookingsList = new List<spaceBookingsModel>();
                    while (reader.Read())
                    {
                        spaceBookingsList.Add(new spaceBookingsModel
                        {
                            Id = (int)reader["Id"],
                            UserId = (int)reader["UserId"],
                            SpaceId = (int)reader["SpaceId"],
                            BookingDate = (DateTime)reader["BookingDate"],
                            StartTime = reader["StartTime"] != DBNull.Value ? (string)reader["StartTime"].ToString() : null,
                            EndTime = reader["EndTime"] != DBNull.Value ? (string)reader["EndTime"].ToString() : null,
                            TotalPrice = (decimal)reader["TotalPrice"],
                            BookingStatus = (string)reader["BookingStatus"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            PaymentStatus = reader["PaymentStatus"] != DBNull.Value ? (string)reader["PaymentStatus"] : "Pending",
                            TransactionId = reader["TransactionId"] != DBNull.Value ? (string)reader["TransactionId"] : null
                        });
                    }
                    return spaceBookingsList;
                }
            }
        }

        public static async Task<int?> getActiveBookings()
        {
            using (SqlCommand command = new SqlCommand("SP_GetActiveBookingsCount"))
            {
                command.CommandType = CommandType.StoredProcedure;

                return await clsPrimaryFunctions.GetScalarAsync(command);


            }
        }

        public static async Task<List<RecentSpaceReservationDTO>> getRecentSpaceReservations()
        {
            
            var reservationsList = new List<RecentSpaceReservationDTO>();

            using (SqlCommand command = new SqlCommand("sp_GetRecentSpaceReservations"))
            {
                command.CommandType = CommandType.StoredProcedure;

                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    
                    while (await reader.ReadAsync())
                    {
                        var reservation = new RecentSpaceReservationDTO
                        {
                            BookingId = (int)reader["BookingId"],
                            UserName = reader["UserName"] != DBNull.Value ? (string)reader["UserName"] : string.Empty,
                            SpaceName = reader["SpaceName"] != DBNull.Value ? (string)reader["SpaceName"] : string.Empty,
                            BookingDate = reader["BookingDate"] != DBNull.Value ? (DateTime)reader["BookingDate"] : DateTime.MinValue,
                            Price = reader["Price"] != DBNull.Value ? (decimal)reader["Price"] : 0,
                            Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : string.Empty
                        };

                        
                        reservationsList.Add(reservation);
                    }
                }
            }

           
            return reservationsList;
        }
    }
}