using InvoiceAPIv2.Models.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using QC = Microsoft.Data.SqlClient;

namespace InvoiceAPIv2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : Controller
    {
        private IConfiguration _config;

        public LoginController(IConfiguration config)
        {
            _config = config;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] UserLogin userLogin)
        {
            var user = AuthenticateUser(userLogin);

            if (user != null)
            {
                var token = GenerateToken(user);
                return Ok(token);
            }

            return NotFound("User not found");
        }

        private string GenerateToken(User user)
        {
            var securityKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                /*new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role)*/
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Audience"],
              claims,
              expires: DateTime.Now.AddMinutes(15),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        private User AuthenticateUser(UserLogin userLogin)
        {
            List<User> users = new List<User>();

            using (var connection = new QC.SqlConnection(
                "Server = LAPTOP-IJL7V72O\\SQLEXPRESS;" +
                "Database = invoiceapidb;" +
                "Trusted_Connection=True; TrustServerCertificate=True;"
                ))
            {
                connection.Open();

                var findByNameCommand = connection.CreateCommand();
                findByNameCommand.CommandText = @"
                SELECT * FROM Users
                WHERE UserName = @UserName;";
                findByNameCommand.Parameters.AddWithValue("@UserName", userLogin.UserName);

                using var reader = findByNameCommand.ExecuteReader();

                while (reader.Read())
                {
                    User user = new User();
                    user.UserId = Int32.Parse(reader[0].ToString());
                    user.UserName = reader[1].ToString();
                    user.Password = reader[2].ToString();

                    users.Add(user);
                }

                reader.Close();


            }

            var currentUser = users.FirstOrDefault(o => o.UserName.ToLower() == userLogin.UserName.ToLower() && o.Password == userLogin.Password);

            if (currentUser != null)
            {
                return currentUser;
            }

            return null;
        }
    }
}
