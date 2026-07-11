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
    public class clsUsersData
    {
        public static async Task<bool> DeleteUsers(int Id)
        {
            using (SqlCommand command = new SqlCommand("Sp_DeleteUserRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", Id);
                return await clsPrimaryFunctions.DeleteAsync(command);
            }
        }

        public static async Task<List<userModel>> GetAllUsers()
        {
            using (SqlCommand command = new SqlCommand("Sp_GetAllUsers"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    var users = new List<userModel>();
                    while (reader.Read())
                    {
                        users.Add(new userModel
                        {
                            Id = (int)reader["Id"],
                            FullName = (string)reader["FullName"],
                            Email = (string)reader["Email"],
                            PasswordHash = (string)reader["PasswordHash"],
                            PasswordSalt = (string)reader["PasswordSalt"],
                            PhoneNumber = reader["PhoneNumber"] != DBNull.Value ? (string)reader["PhoneNumber"] : null,
                            IsEmailConfirmed = (bool)reader["IsEmailConfirmed"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            IsSuspended = (bool)reader["IsSuspended"] 
                        });
                    }

                    return users;
                }
            }
        }

        public static bool FindByID(userModel model)
        {
            bool isFound = false;
            using (SqlConnection connection = new SqlConnection(clsPrimaryFunctions.connectionString))
            {
                using (SqlCommand command = new SqlCommand("Sp_GetUsersByID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", model.Id);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isFound = true;
                                model.Id = (int)reader["ID"];
                                model.FullName = (string)reader["FullName"];
                                model.Email = (string)reader["Email"];
                                model.PasswordHash = (string)reader["PasswordHash"];
                                model.PasswordSalt = (string)reader["PasswordSalt"];
                                model.PhoneNumber = reader["PhoneNumber"] != DBNull.Value ? (string)reader["PhoneNumber"] : null;
                                model.IsEmailConfirmed = (bool)reader["IsEmailConfirmed"];
                                model.CreatedAt = (DateTime)reader["CreatedAt"];
                                model.IsSuspended = (bool)reader["IsSuspended"]; 
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

        public static async Task<int?> AddNewUsers(userModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_AddNewUsers"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@FullName", model.FullName);
                command.Parameters.AddWithValue("@Email", model.Email);
                command.Parameters.AddWithValue("@PasswordHash", model.PasswordHash);
                command.Parameters.AddWithValue("@PasswordSalt", model.PasswordSalt);
                command.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                command.Parameters.AddWithValue("@IsEmailConfirmed", model.IsEmailConfirmed);
                command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);
                command.Parameters.AddWithValue("@IsSuspended", model.IsSuspended);
                command.Parameters.Add("@NewAddClass", SqlDbType.Int).Direction = ParameterDirection.Output;

                return await clsPrimaryFunctions.AddAsync(command, "@NewAddClass");
            }
        }

        public static async Task<bool?> UpdateUsers(userModel model)
        {
            using (SqlCommand command = new SqlCommand("Sp_UpdateUsers"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Id", model.Id);
                command.Parameters.AddWithValue("@FullName", model.FullName);
                command.Parameters.AddWithValue("@Email", model.Email);
                command.Parameters.AddWithValue("@PasswordHash", model.PasswordHash);
                command.Parameters.AddWithValue("@PasswordSalt", model.PasswordSalt);
                command.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                command.Parameters.AddWithValue("@IsEmailConfirmed", model.IsEmailConfirmed);
                command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);
                command.Parameters.AddWithValue("@IsSuspended", model.IsSuspended);

                return await clsPrimaryFunctions.UpdateAsync(command);
            }
        }

        public static async Task<userModel> FindByEmail(string email)
        {
            using (SqlCommand command = new SqlCommand("Sp_GetUsersByEmail"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Email", email);
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    if (reader.Read())
                    {
                        return new userModel
                        {
                            Id = (int)reader["Id"],
                            FullName = (string)reader["FullName"],
                            Email = (string)reader["Email"],
                            PasswordHash = (string)reader["PasswordHash"],
                            PasswordSalt = (string)reader["PasswordSalt"],
                            PhoneNumber = reader["PhoneNumber"] != DBNull.Value ? (string)reader["PhoneNumber"] : null,
                            IsEmailConfirmed = (bool)reader["IsEmailConfirmed"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            IsSuspended = (bool)reader["IsSuspended"] 
                        };
                    }
                }
            }
            return null;
        }

        public static async Task<int?> getTotalMembersCount()
        {
            using (SqlCommand command = new SqlCommand("sp_GetTotalMembersCount"))
            {
                command.CommandType = CommandType.StoredProcedure;
                return await clsPrimaryFunctions.GetScalarAsync(command);
            }
        }

        public static async Task<List<UserWithRoleDto>> getUsersWhitRole()
        {
            using (SqlCommand command = new SqlCommand("sp_GetAllUsersWithRoles"))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = await clsPrimaryFunctions.GetAsync(command))
                {
                    var users = new List<UserWithRoleDto>();
                    while (reader.Read())
                    {
                        users.Add(new UserWithRoleDto
                        {
                            UserId = (int)reader["UserId"],
                            FullName = (string)reader["FullName"],
                            Email = (string)reader["Email"],
                            RoleId = (int)reader["RoleId"],
                            JoinDate = (DateTime)reader["JoinDate"],
                            IsSuspended = (bool)reader["IsSuspended"] 
                        });
                    }

                    return users;
                }
            }
        }
    }
}