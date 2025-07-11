using LoanTrackerJLC.Data;
using LoanTrackerJLC.DTOs;
using LoanTrackerJLC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LoanTrackerJLC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly LoanTrackerJLCDbContext _context;

        public AuthController(LoanTrackerJLCDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> MobileLogin([FromBody] MobileLoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == dto.MobileNumber);

            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid mobile number." });
            }

            // For now, we'll just return the user's details.
            // In a real scenario, you would implement OTP sending here.
            return Ok(new LoginResponseDto {
                Message = "Login successful.",
                UserId = user.UserId,
                UserType = user.UserType ?? 0,
                RequiresMpinSetup = user.PIN == null
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
        {
            // TODO: Implement OTP verification logic
            // 1. Find user by mobile number
            // 2. Retrieve stored OTP (e.g., from cache, database, or a temporary store)
            // 3. Compare provided OTP with stored OTP
            // 4. If valid, proceed to MPIN setup or login.

            // Placeholder:
            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == dto.MobileNumber);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (dto.Otp == "321456") // Replace with actual OTP validation
            {
                // OTP is valid
                // Check if MPIN needs to be created or if user can log in
                if (user.PIN == null) // Assuming PIN is the MPIN
                {
                    return Ok(new { Message = "OTP verified. Please create MPIN.", RequiresMpinSetup = true });
                }
                // Generate a token for authenticated session
                // string token = GenerateJwtToken(user); // Implement token generation
                return Ok(new { Message = "Login successful.", /*Token = token,*/ UserId = user.UserId, UserType = user.UserType ?? 0 });
            }

            return BadRequest(new { Message = "Invalid OTP." });
        }

        [HttpPost("create-mpin")]
        public async Task<IActionResult> CreateMpin([FromBody] CreateMpinRequestDto dto)
        {
            // TODO: Implement MPIN creation logic
            // 1. Find user by mobile number (or UserId if passed after OTP verification)
            // 2. Ensure MPIN meets complexity requirements (e.g., 4 or 6 digits)
            // 3. Hash and store the MPIN securely

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == dto.MobileNumber);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (user.PIN != null)
            {
                return BadRequest(new { Message = "MPIN already set for this user." });
            }

            // Basic validation for MPIN length (e.g., 4 digits)
            if (dto.Mpin == null || dto.Mpin.ToString().Length != 4)
            {
                 return BadRequest(new { Message = "MPIN must be 4 digits."});
            }

            user.PIN = dto.Mpin; // In a real app, hash this PIN before saving
            _context.tblUsers.Update(user);
            await _context.SaveChangesAsync();

            // string token = GenerateJwtToken(user); // Implement token generation
            return Ok(new { Message = "MPIN created successfully. Login successful.", /*Token = token,*/ UserId = user.UserId, UserType = user.UserType ?? 0 });
        }

        [HttpPost("mpin-login")]
        public async Task<IActionResult> MpinLogin([FromBody] MpinLoginRequestDto dto)
        {
            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == dto.MobileNumber);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (user.PIN == null)
            {
                return BadRequest(new { Message = "MPIN not set for this user. Please login with OTP to set MPIN." });
            }

            // In a real app, compare hashed MPIN
            if (user.PIN == dto.Mpin)
            {
                // string token = GenerateJwtToken(user); // Implement token generation
                return Ok(new { Message = "MPIN login successful.", /*Token = token,*/ UserId = user.UserId, UserType = user.UserType ?? 0 });
            }

            return Unauthorized(new { Message = "Invalid MPIN." });
        }

        // TODO: Add helper method for JWT token generation if needed
        // private string GenerateJwtToken(tblUser user) { ... }
    }
}