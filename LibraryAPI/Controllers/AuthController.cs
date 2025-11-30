using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(LibraryDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new { 
                status = "API is running", 
                endpoint = "Auth API",
                message = "Use POST /api/Auth/login to login" 
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.ReaderCard)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Account is disabled" });
            }

            var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                ReaderCardCode = user.ReaderCard?.CardCode
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Username, password, and email are required" });
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Create new user (only Reader role for registration)
            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Email = request.Email,
                Role = "Reader",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create ReaderCard for the new user
            var cardCode = GenerateCardCode();
            var readerCard = new ReaderCard
            {
                UserId = user.Id,
                CardCode = cardCode,
                FullName = request.FullName ?? request.Username,
                Phone = request.Phone,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReaderCards.Add(readerCard);
            await _context.SaveChangesAsync();

            // Generate token and return login response
            var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                ReaderCardCode = readerCard.CardCode
            });
        }

        private string GenerateCardCode()
        {
            // Generate a unique card code
            var random = new Random();
            var code = $"RC{random.Next(100000, 999999)}";
            
            // Ensure uniqueness
            while (_context.ReaderCards.Any(rc => rc.CardCode == code))
            {
                code = $"RC{random.Next(100000, 999999)}";
            }

            return code;
        }
    }
}

