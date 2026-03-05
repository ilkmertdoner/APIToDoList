using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagerApi.Data;
using TaskManagerApi.Models;
using Google.Apis.Auth;
using System.Text.Json;
using TaskManagerApi.Dto;

namespace TaskManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AuthController(AppDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Bu e-posta adresi zaten kullanılıyor.");

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Bu kullanıcı adı zaten kullanılıyor.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            var emailToken = new EmailToken
            {
                Email = request.Email,
                Username = request.Username,
                Password = user.Password,
                Code = new Random().Next(100000, 999999).ToString(),
                ExpirationDate = DateTime.Now.AddMinutes(15)
            };

            _context.EmailTokens.Add(emailToken);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kayıt başarılı. Lütfen e-postanızı doğrulayın." });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] EmailToken request)
        {
            var tokenRecord = await _context.EmailTokens.FirstOrDefaultAsync
                (t => t.Email == request.Email && t.Code == request.Code);

            if (tokenRecord == null)
                return BadRequest("Hatalı doğrulama kodu.");

            if (tokenRecord.ExpirationDate < DateTime.Now)
                return BadRequest("Doğrulama kodunun süresi dolmuş.");

            var user = new User
            {
                Username = tokenRecord.Username,
                Email = tokenRecord.Email,
                Password = tokenRecord.Password
            };

            _context.Users.Add(user);
            _context.EmailTokens.Remove(tokenRecord);
            await _context.SaveChangesAsync();

            return Ok(new { message = "E-posta başarıyla doğrulandı." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return BadRequest("Geçersiz kullanıcı adı veya şifre.");

            var token = GenerateJwtToken(user);
            return Ok(new { token = token, username = user.Username });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { "787940789409-k70mn4qf4fatqgsjnlr3h7fn8dj7bklt.apps.googleusercontent.com" }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                    return BadRequest("Bu Google hesabı sistemimizde kayıtlı değil. Lütfen önce kayıt olun.");

                var token = GenerateJwtToken(user);
                return Ok(new { token = token, username = user.Username });
            }
            catch
            {
                return BadRequest("Google girişi doğrulanamadı.");
            }
        }

        [HttpPost("google-register")]
        public async Task<IActionResult> GoogleRegister([FromBody] GoogleAuthDto request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { "787940789409-k70mn4qf4fatqgsjnlr3h7fn8dj7bklt.apps.googleusercontent.com" }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user != null)
                    return BadRequest("Bu e-posta adresi zaten kayıtlı. Lütfen giriş yapın.");

                user = new User
                {
                    Username = payload.Email.Split('@')[0],
                    Email = payload.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                return Ok(new { token = token, username = user.Username });
            }
            catch
            {
                return BadRequest("Google kaydı doğrulanamadı.");
            }
        }

        [HttpGet("microsoft-login")]
        public IActionResult MicrosoftLogin()
        {
            var clientId = _configuration["ClientId"];
            var tenantId = _configuration["TenantId"];
            var redirectUri = "https://localhost:7133/api/Auth/microsoft-callback";
            var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?client_id={clientId}" +
                $"&response_type=code&redirect_uri={redirectUri}&scope=User.Read openid email profile" +
                $"&response_mode=query&state=login";

            return Redirect(url);
        }

        [HttpGet("microsoft-register")]
        public IActionResult MicrosoftRegister()
        {
            var clientId = _configuration["ClientId"];
            var tenantId = _configuration["TenantId"];
            var redirectUri = "https://localhost:7133/api/Auth/microsoft-callback";
            var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?client_id={clientId}" +
                $"&response_type=code&redirect_uri={redirectUri}&scope=User.Read openid email profile" +
                $"&response_mode=query&state=register";

            return Redirect(url);
        }

        [HttpGet("microsoft-callback")]
        public async Task<IActionResult> MicrosoftCallback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Kod alınamadı.");

            var clientId = _configuration["ClientId"];
            var clientSecret = _configuration["ClientSecret"];
            var tenantId = _configuration["TenantId"];
            var redirectUri = "https://localhost:7133/api/Auth/microsoft-callback";

            var tokenResponse = await _httpClient.PostAsync($"https://login.microsoftonline.com/{tenantId}" +
                $"/oauth2/v2.0/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    {"client_id", clientId},
                    {"scope", "User.Read openid email profile"},
                    {"code", code},
                    {"redirect_uri", redirectUri},
                    {"grant_type", "authorization_code"},
                    {"client_secret", clientSecret}
            }));

            if (!tokenResponse.IsSuccessStatusCode) return BadRequest("Microsoft token alınamadı.");

            var tokenData = await tokenResponse.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(tokenData);
            var accessToken = jsonDocument.RootElement.GetProperty("access_token").GetString();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue
                ("Bearer", accessToken);

            var userResponse = await _httpClient.SendAsync(requestMessage);

            if (!userResponse.IsSuccessStatusCode) return BadRequest("Kullanıcı bilgileri alınamadı.");

            var userData = await userResponse.Content.ReadAsStringAsync();
            var userJson = JsonDocument.Parse(userData);

            var email = userJson.RootElement.TryGetProperty("mail", out var mailProp) 
                && mailProp.ValueKind != JsonValueKind.Null
                ? mailProp.GetString()
                : userJson.RootElement.GetProperty("userPrincipalName").GetString();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (state == "login" && user == null)
            {
                var errorHtml = @"
                    <script>
                        alert('Bu Microsoft hesabı sistemimizde kayıtlı değil. Lütfen önce kayıt olun.');
                        window.location.href = 'http://127.0.0.1:5500/index.html';
                    </script>";
                return Content(errorHtml, "text/html");
            }

            if (state == "register" && user != null)
            {
                var errorHtml = @"
                    <script>
                        alert('Bu Microsoft hesabı zaten kayıtlı. Lütfen giriş yapın.');
                        window.location.href = 'http://127.0.0.1:5500/index.html';
                    </script>";
                return Content(errorHtml, "text/html");
            }

            if (user == null)
            {
                user = new User
                {
                    Username = email.Split('@')[0],
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var jwt = GenerateJwtToken(user);

            var html = $@"
                <script>
                    localStorage.setItem('jwtToken', '{jwt}');
                    localStorage.setItem('username', '{user.Username}');
                    window.location.href = 'http://127.0.0.1:5500/index.html';
                </script>";

            return Content(html, "text/html");
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim("Username", user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}