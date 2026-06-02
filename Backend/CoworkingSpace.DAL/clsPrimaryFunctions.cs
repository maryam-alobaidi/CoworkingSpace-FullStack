using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;

using System.Diagnostics;

namespace CoworkingSpace.DAL
{
    
      public static class  clsPrimaryFunctions
      {
      
        public static string? connectionString;

        public static void Initialize(string connectionString)
        {
            clsPrimaryFunctions.connectionString = connectionString;
        }

        public static async Task<int?> AddAsync(SqlCommand command, string outputParameterName)
        {
            int? ID = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                command.Connection = connection;

                try
                {
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    SqlParameter? outputParam = command.Parameters[outputParameterName] as SqlParameter;

                    if (outputParam != null && outputParam.Value != DBNull.Value)
                    {
                        if (int.TryParse(outputParam.Value.ToString(), out int insertedID))
                        {
                            ID = insertedID;
                        }
                        else
                        {
                            throw new Exception($"Output parameter '{outputParameterName}' is not a valid integer value.");
                        }
                    }
                    else
                    {
                        throw new Exception($"Output parameter '{outputParameterName}' is null or not found in the command parameters.");
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                return ID;
            }
        }

        public static async Task<bool> DeleteAsync(SqlCommand command)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                command.Connection = connection;

                try
                {
                    await connection.OpenAsync();
                    int rowAffected = await command.ExecuteNonQueryAsync();
                    return rowAffected > 0;
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Message.Contains("No records found to delete for the provided ID."))
                    {
                        return false;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public static async Task<bool> UpdateAsync(SqlCommand command)
        {
            int rowAffected = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                command.Connection = connection;

                try
                {
                    await connection.OpenAsync();
                    rowAffected = await command.ExecuteNonQueryAsync();
                }
                catch (SqlException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new Exception("An unexpected error occurred during the update database operation.", ex);
                }
            }

            return rowAffected > 0;
        }

        public static async Task<SqlDataReader> GetAsync(SqlCommand command)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            command.Connection = connection;
            SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            return reader;
        }

        public static async Task<bool> AddUserRoleAsync(SqlCommand command)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    command.Connection = connection;
                    await connection.OpenAsync();

                    // نستخدم ExecuteScalar لأننا نريد التأكد من إتمام الإضافة (يرجع 1 أو 0)
                    object result = await command.ExecuteScalarAsync();

                    return result != null && Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ في الـ Log الخاص بكِ
                EntireInfoToEventLoge(ex.Message);
                return false;
            }
        }

        public static void EntireInfoToEventLoge(string message)
        {
            string source = "CoworkingSpaceApp";
            string log = "Application";

            try
            {
                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, log);
                }
                EventLog.WriteEntry(source, message, EventLogEntryType.Error);
            }
            catch
            {
                Console.WriteLine("Logging failed: " + message);
            }
        }



      }

  }


