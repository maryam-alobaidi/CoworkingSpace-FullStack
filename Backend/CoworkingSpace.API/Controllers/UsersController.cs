using CoworkingSpace.BLL;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UsersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] CreateUserModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid user data.");
            }

            string salt = clsPasswordHasher.GenerateSalt();
            string passwordHash = clsPasswordHasher.ComputeHash(model.Password, salt);

            clsUsers user = new clsUsers
            {
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                PhoneNumber = model.PhoneNumber
            };

            bool isAdded = await user.Save();
            if (!isAdded)
            {
                return StatusCode(500, "An error occurred while adding the user.");
            }

            int finalRoleId = model.RoleId ?? 5;
            
            userRoleModel roleModel = new userRoleModel
            {
                UserId = user.Id,
                RoleId = finalRoleId
            };

            bool isRoleAssigned = await clsUserRoles.AddUserRole(roleModel);
            if (!isRoleAssigned)
            {
                return StatusCode(500, "An error occurred while assigning the role to the user.");
            }

            return Ok(new { message = "User added successfully." });
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Email and password are required.");
            }

            clsUsers user = await clsUsers.FindByEmail(model.Email);

            if (user == null || !clsPasswordHasher.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                return Unauthorized("Invalid email or password.");
            }

           
            string userRole = await clsUserRoles.GetRoleNameByUserId(user.Id) ?? "User";

            // تمرير الـ Role إلى دالة توليد التوكن
            var token = GenerateJwtToken(user, userRole);

            // 🌟 الخطوة 2: إرجاع الـ Role في الـ Response ليفهمه الأنجولار فوراً
            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                Role = userRole, // أرسلنا "Admin" أو "User"
                user.IsSuspended,
                Token = token
            });
        }

        private string GenerateJwtToken(clsUsers user, string role)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT key is not configured.");
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, role) 
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool isDeleted = await clsUsers.Delete(id);
            if (!isDeleted)
            {
                return NotFound("User not found or could not be deleted.");
            }
            return Ok("User deleted successfully.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Email))
            {
                return BadRequest("Invalid user data.");
            }
            clsUsers user = clsUsers.Find(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            
            bool isUpdated = await user.Save();
            if (!isUpdated)
            {
                return StatusCode(500, "An error occurred while updating the user.");
            }
            return Ok("User updated successfully.");
        }

      

        [HttpGet]
      
        public async Task<List<userModel>> GetAllUsers()
        {
            return await clsUsers.GetAllUsers();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = clsUsers.Find(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return Ok(new userModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            });
        }

        [HttpGet("total-members")]
         public async Task<IActionResult> GetTotalMembersCount()
        {
            try
            {
                int? countTotalMembers = await clsUsers.getTotalMembersCount();
                return Ok(new { countTotalMembers });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while return the count total members.", error = ex.Message });
            }
        }

        [HttpGet("with-role")]
        public async Task<IActionResult> GetUsersWhitRole()
        {
            try
            {
                var users = await clsUsers.getUsersWhitRole();
                if (users == null) return NotFound("No users found.");
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("toggle-suspend/{Id}")]
        public async Task<IActionResult> ToggleSuspend(int Id)
        {
            try
            {
                
                bool isSuccess = await clsUsers.ToggleSuspend(Id);

                if (isSuccess)
                {
                    
                    var user = clsUsers.Find(Id);
                    string message = user != null && user.IsSuspended
                        ? "User account has been suspended successfully."
                        : "User account has been activated successfully.";

                    return Ok(new { message = message });
                }

                
                return BadRequest(new { message = "Failed to update user status. User might not exist." });
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { message = "An error occurred: " + ex.Message });
            }
        }
    }
}