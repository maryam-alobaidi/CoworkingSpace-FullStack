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
        // Add this field to the UsersController class
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

            userRoleModel roleModel = new userRoleModel
            {
                UserId = user.Id,
                RoleId = 5 // Assuming 5 is the ID for member role in my system
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



            var token = GenerateJwtToken(user);

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                Token = token // token is included in the response
            });
        }



        // Helper method to generate JWT token
        private string GenerateJwtToken(clsUsers user)
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
                new Claim(ClaimTypes.Name, user.FullName)
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
    }
}