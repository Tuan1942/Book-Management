using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookManagement.Models;
using Microsoft.AspNetCore.Identity;
using BookManagement.Contexts;

namespace BookManagement.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly BMContext _context;
        private readonly IConfiguration _configuration;

        public UserController(BMContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel rUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = new User
            {
                Username = rUser.Username,
                Password = HashPassword(rUser.Password) 
            };

            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                return BadRequest("Tên người dùng đã tồn tại.");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserModel lUser)
        {
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == lUser.Username);
            if (dbUser == null || !VerifyPassword(lUser.Password, dbUser.Password))
            {
                return Unauthorized("Thông tin không hợp lệ.");
            }

            var token = GenerateJwtToken(dbUser, _configuration);
            return Ok(token);
        }

        public string AddCookie(User user, IConfiguration configuration)
        {
            var token = GenerateJwtToken(user, configuration);
            HttpContext.Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax
            });
            return token;
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null)
            {
                return BadRequest("Token không hợp lệ.");
            }
            /*
            var blacklistToken = new TokenBlacklist
            {
                Token = token,
                Expiration = jwtToken.ValidTo
            };

            _context.TokenBlacklists.Add(blacklistToken);
            await _context.SaveChangesAsync();
            */
            HttpContext.Response.Cookies.Delete("jwtToken");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("current")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = HttpContext.User.Identity.Name;

            var roles = HttpContext.User.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList();

            return Ok(new
            {
                Id = userId,
                Username = username,
                Roles = roles
            });
        }
        [HttpGet("admin-only")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAdminOnlyData()
        {
            return Ok(new { Message = "This data is accessible only by admin." });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }

        public string GenerateJwtToken(User user, IConfiguration configuration)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };
            var userRoles = GetUserRolesFromDatabase(user.Id);

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private List<string> GetUserRolesFromDatabase(int userId)
        {
            var roles = new List<string>();
            var userRoles = _context.UserRoles.Where(ur => ur.UserId == userId).ToList();
            foreach (var userRole in userRoles)
            {
                roles.Add(_context.Roles.First(r => r.Id == userRole.RoleId).Name);
            }
            return roles;
        }

        [HttpGet("list")]
        [Authorize]
        public async Task<IActionResult> GetListUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userList = await _context.Users
                .Where(u => u.Id != int.Parse(userIdClaim))
                .Select(u => new {
                    u.Id,
                    u.Username
                })
                .ToListAsync();
            return Ok(userList);
        }
    }
}
