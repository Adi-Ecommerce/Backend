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

        // REGISTER
        [HttpPost("register")]
        public IActionResult Register(RegisterUserDto dto)
        {
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

            return Ok(new { message = "User registered successfully!" });
        }

        // LOGIN
        [HttpPost("login")]
        public IActionResult Login(LoginUserDto dto)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate JWT
            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful!",
                token = token
            });
        }
    }
}
