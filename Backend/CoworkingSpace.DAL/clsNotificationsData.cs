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
    public static class clsNotificationsData
    {

        public static async Task<int?> AddNewNotifications(notificationsModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddNewNotifications"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserID", model.UserID);
                command.Parameters.AddWithValue("@Title", model.Title);

               
                command.Parameters.AddWithValue("@Message", (object)model.Message ?? DBNull.Value);
                command.Parameters.AddWithValue("@NotificationType", (object)model.NotificationType ?? DBNull.Value);
                command.Parameters.AddWithValue("@TargetURL", (object)model.TargetURL ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsRead", model.IsRead);
                command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

               
                command.Parameters.AddWithValue("@ReadAt", (object)model.ReadAt ?? DBNull.Value);

                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;

                return await clsPrimaryFunctions.AddAsync(command, "@NewAddClass");
            }
        }

        public static async Task<bool> MarkNotificationAsRead(int NotificationID)
        {
            using (SqlCommand command = new SqlCommand("Sp_MarkNotificationAsRead"))
            {
                command.CommandType = CommandType.StoredProcedure;

               
                command.Parameters.Add("@NotificationID", SqlDbType.Int).Value = NotificationID;

                return await clsPrimaryFunctions.ExecuteNonQueryAsync(command);
            }
        }


        public static async Task<List<notificationsModel>> GetAllNotificationsByUserID(int UserId)
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllNotifications"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@UserID", SqlDbType.Int).Value = UserId;

                List<notificationsModel> notificationsList = new List<notificationsModel>();

              
                using (var reader = await clsPrimaryFunctions.GetNotificationsListAsync(command))
                {
                    if (reader != null)
                    {

                        while (await reader.ReadAsync())
                        {
                            notificationsList.Add(new notificationsModel
                            {
                                NotificationID = reader["NotificationID"] != DBNull.Value ? (int)reader["NotificationID"] : -1,
                                UserID = UserId,
                                Title = reader["Title"] != DBNull.Value ? (string)reader["Title"] : null,
                                Message = reader["Message"] != DBNull.Value ? (string)reader["Message"] : null,
                                NotificationType = reader["NotificationType"] != DBNull.Value ? (string)reader["NotificationType"] : null,
                                TargetURL = reader["TargetURL"] != DBNull.Value ? (string)reader["TargetURL"] : null,
                                IsRead = false, // 🌟 نضع false مباشرة لأن الاستعلام يرجع فقط IsRead = 0
                                CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : default(DateTime),
                                ReadAt = null // 🌟 نضع null مباشرة لأنه لم يقرأ بعد
                            });
                        }
                    }
                    
                }
                return notificationsList;
            }
        }
      

      

    }
}
