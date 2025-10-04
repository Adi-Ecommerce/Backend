using Backend.Data;
using Backend.Model.Entities;
using Backend.Model.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly JwtService _jwtService;

        public UsersController(AuthDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // ✅ REGISTER
        [HttpPost("register")]
        public IActionResult Register(RegisterUserDto dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email already exists" });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Age = dto.Age,
                Address = dto.Address,
                Email = dto.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // 🔑 Optional: auto-generate token right after registration
            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "User registered successfully!",
                user = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email
                },
                token
            });
        }

        //  LOGIN
        [HttpPost("login")]
        public IActionResult Login(LoginUserDto dto)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Invalid email or password" });

            // Generate JWT
            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful!",
                token,
                user = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email
                }
            });
        }
    }
}