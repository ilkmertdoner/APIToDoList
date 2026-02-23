using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManagerApi.Data;
using TaskManagerApi.Models;

namespace TaskManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] User user)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest("Username already exists.");

            if (user.Password.Length < 6) return BadRequest("Password needs to be more than 6 characters.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Successfully Registered!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] User user)
        {
            var userFromDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == user.Username);

            if (userFromDb == null || !BCrypt.Net.BCrypt.Verify(user.Password, userFromDb.Password))
                return Unauthorized("Invalid username or password.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var claims = new List<Claim>
            {
                new Claim("UserId", userFromDb.Id.ToString()),
                new Claim("UserName", userFromDb.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6.30),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                userId = userFromDb.Id,
                username = userFromDb.Username
            });
        }
    }
}
