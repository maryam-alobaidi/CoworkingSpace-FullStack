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
    public static class clsPaymentsData
    {

        public static async Task<int?> AddNewPayments(paymentsModel model)
        {

            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_AddNewPayments", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@ReferenceID", model.ReferenceID);
                    command.Parameters.AddWithValue("@ReferenceType", model.ReferenceType);
                    command.Parameters.AddWithValue("@Amount", model.Amount);
                    command.Parameters.AddWithValue("@Currency", model.Currency);
                    command.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod);
                    command.Parameters.AddWithValue("@TransactionID", model.TransactionID);
                    command.Parameters.AddWithValue("@PaymentStatus", model.PaymentStatus);
                    command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

                    // إضافة الـ Output Parameter
                    SqlParameter outputIdParam = new SqlParameter("@NewAddClass", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputIdParam);

                    try
                    {
                        // فتح الاتصال والتنفيذ بأمان
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();

                        if (outputIdParam.Value != DBNull.Value)
                        {
                            return (int)outputIdParam.Value;
                        }

                        return -1;
                    }
                    catch (Exception ex)
                    {
                        // تسجيل الخطأ الفعلي لتتمكني من رؤيته في لوحة تحكم السيرفر
                        clsPrimaryFunctions.EntireInfoToEventLoge($"Error in AddNewPayments: {ex.Message}");
                        return -1;
                    }
                }
            }
        }

        public static async Task<bool> DeletePayments(int PaymentID)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeletePayments"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@PaymentID", PaymentID);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<List<paymentsModel>> GetAllPayments()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllPayments"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    List<paymentsModel> paymentsList = new List<paymentsModel>();
                    while (reader.Read())
                    {
                        paymentsModel model = new paymentsModel
                        {
                            PaymentID = (int)reader["PaymentID"],
                            ReferenceID = (int)reader["ReferenceID"],
                            ReferenceType = (string)reader["ReferenceType"],
                            Amount = (decimal)reader["Amount"],
                            Currency = reader["Currency"] != DBNull.Value ? (string)reader["Currency"] : null,
                            PaymentMethod = reader["PaymentMethod"] != DBNull.Value ? (string)reader["PaymentMethod"] : null,
                            TransactionID = reader["TransactionID"] != DBNull.Value ? (string)reader["TransactionID"] : null,
                            PaymentStatus = (string)reader["PaymentStatus"],
                            CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : default(DateTime)
                        };
                        paymentsList.Add(model);
                    }
                    return paymentsList;
                }
            }
        }

        public static bool FindByID(int PaymentID, paymentsModel model)
        {
            bool isFound = false;
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetPaymentsByID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PaymentID", PaymentID);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isFound = true;
                                model.ReferenceID = (int)reader["ReferenceID"];
                                model.ReferenceType = (string)reader["ReferenceType"];
                                model.Amount = (decimal)reader["Amount"];
                                model.Currency = reader["Currency"] != DBNull.Value ? (string)reader["Currency"] : null;
                                model.PaymentMethod = reader["PaymentMethod"] != DBNull.Value ? (string)reader["PaymentMethod"] : null;
                                model.TransactionID = reader["TransactionID"] != DBNull.Value ? (string)reader["TransactionID"] : null;
                                model.PaymentStatus = (string)reader["PaymentStatus"];
                                model.CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : default(DateTime);

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

        public static async Task<bool?> UpdatePayments(paymentsModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_UpdatePayments"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@PaymentID", model.PaymentID);
                command.Parameters.AddWithValue("@ReferenceID", model.ReferenceID);
                command.Parameters.AddWithValue("@ReferenceType", model.ReferenceType);
                command.Parameters.AddWithValue("@Amount", model.Amount);
                command.Parameters.AddWithValue("@Currency", model.Currency);
                command.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod);
                command.Parameters.AddWithValue("@TransactionID", model.TransactionID);
                command.Parameters.AddWithValue("@PaymentStatus", model.PaymentStatus);
                command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }


        public static async Task<decimal> GetTotalRevenue()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetTotalRevenue"))
            {
                command.CommandType = CommandType.StoredProcedure;
                object result = await clsPrimaryFunctions.ExecuteScalarAsync(command);
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
                return 0m; // Return 0 if no revenue is found
            }
        }

    }
}
