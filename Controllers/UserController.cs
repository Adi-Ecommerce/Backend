
using Backend.Data;
using Backend.Model.Entities;
using Backend.Model.DTOs;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using System.Linq;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public UsersController(AuthDbContext context)
        {
            _context = context;
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
            // 1. Find user by email
            var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // 2. Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // 3. Return success
            return Ok(new { message = "Login successful!" });
        }
    }
}

