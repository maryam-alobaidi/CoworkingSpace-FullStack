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
            // 1️⃣ نقوم بإنشاء الاتصال بشكل صريح هنا لضمان عدم حدوث تضارب في الـ Threads
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_AddNewNotifications", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@UserID", model.UserID);
                    command.Parameters.AddWithValue("@Title", model.Title);
                    command.Parameters.AddWithValue("@Message", (object)model.Message ?? DBNull.Value);
                    command.Parameters.AddWithValue("@NotificationType", (object)model.NotificationType ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TargetURL", (object)model.TargetURL ?? DBNull.Value);

                    // تأمين إرسال حقل الـ IsRead 
                    command.Parameters.AddWithValue("@IsRead", model.IsRead);

                    // تأمين حقل تاريخ الإنشاء
                    command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt ?? DateTime.Now);
                    command.Parameters.AddWithValue("@ReadAt", (object)model.ReadAt ?? DBNull.Value);

                    // تحديد الـ Output Parameter لجلب المعرّف الجديد
                    SqlParameter outputIdParam = new SqlParameter("@NewAddClass", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputIdParam);

                    try
                    {
                        // فتح الاتصال وتنفيذ الأمر بشكل غير متزامن بالكامل (Async)
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();

                        // استخراج القيمة المسترجعة بأمان
                        if (outputIdParam.Value != DBNull.Value)
                        {
                            return (int)outputIdParam.Value;
                        }

                        return -1;
                    }
                    catch (Exception ex)
                    {
                        // تسجيل الخطأ الفعلي إن وجد لكي يظهر لك في الـ Event Log الخاص بالسيرفر
                        clsPrimaryFunctions.EntireInfoToEventLoge($"Error in AddNewNotifications: {ex.Message}");
                        return -1;
                    }
                }
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
