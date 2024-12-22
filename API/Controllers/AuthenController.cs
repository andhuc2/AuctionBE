using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NET_base.Models;
using NET_base.Models.Common;
using NET_base.Models.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secret;
        private readonly int _expirationHours;

        public AuthenController(IConfiguration configuration, DBContext context)
        {
            _configuration = configuration;
            _secret = configuration["JwtSettings:Secret"];
            _expirationHours = int.Parse(configuration["JwtSettings:ExpirationHours"]);
            _context = context;
        }

        // Authentication - Login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<Response<string>> Authen(AuthenDTO dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null || !VerifyPassword(user.Password, dto.Password))
                {
                    return new Response<string>(false, "Invalid username or password", null);
                }

                var token = GenerateJwtToken(user);
                return new Response<string>(true, Constant.SUCCESS_MESSAGE, token);
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                return new Response<string>(false, Constant.FAIL_MESSAGE, null);
            }
        }

        // Registration - Register new user
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<Response<bool>> Register(AuthenDTO dto)
        {
            try
            {
                // Check if the user already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                {
                    return new Response<bool>(false, "User already exists with the given email", false);
                }

                // Create a new user
                var newUser = new User
                {
                    Username = dto.Email,
                    Email = dto.Email,
                    Password = HashPassword(dto.Password),
                    FullName = dto.Email,
                    Role = Constant.USER_ROLE,
                    IsDeleted = false
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return new Response<bool>(true, "User registered successfully.", true);
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                return new Response<bool>(false, "An error occurred during registration", false);
            }
        }

        // Helper function to generate JWT Token
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", user.Id.ToString(), ClaimValueTypes.Integer),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString(), ClaimValueTypes.Integer),
                }),
                Expires = DateTime.UtcNow.AddHours(_expirationHours),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            var passwordHasher = new PasswordHasher<object>();
            return passwordHasher.HashPassword(null, password);
        }

        private bool VerifyPassword(string hashedPassword, string password)
        {
            var passwordHasher = new PasswordHasher<object>();
            var result = passwordHasher.VerifyHashedPassword(null, hashedPassword, password);
            return result == PasswordVerificationResult.Success;
        }
    }
}
