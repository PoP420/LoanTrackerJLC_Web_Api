using LoanTrackerJLC.Data;
using LoanTrackerJLC.DTOs;
using LoanTrackerJLC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Net.Http;
using System;

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

            // Check if OTP is required (7 days interval)
           bool requiresOtp = true;

            if (user.LastOtpVerification == null)
            {
                // Make sure OtpCreatedAt is not null before accessing its value
                if (user.OtpCreatedAt.HasValue)
                {
                    requiresOtp = (DateTime.UtcNow - user.OtpCreatedAt.Value).TotalMinutes > 300;
                }
            }
            else
            {
                var daysSinceLastOtp = (DateTime.UtcNow - user.LastOtpVerification.Value).TotalDays;
                requiresOtp = daysSinceLastOtp >= 30; // Configurable
            }

            if (!requiresOtp)
            {
                return Ok(new LoginResponseDto
                {
                    Message = "OTP not required. Proceed to MPIN.",
                    UserId = user.UserId,
                    UserType = user.UserType ?? 0,
                    RequiresMpinSetup = user.PIN == null,
                    RequiresOtp = false // Added to DTO
                });
            }

            // Generate and store OTP
            string otp = new Random().Next(100000, 999999).ToString();
            user.Otp = otp; // Hash in production
            user.OtpCreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send OTP via SMSSender
            string message = $"Your OTP is {otp}. Valid for 5 minutes.";
            var smsResult = await SMSSender(dto.MobileNumber, message);
            if (smsResult != null)
            {
                return Ok(new LoginResponseDto
                {
                    Message = "OTP sent successfully.",
                    UserId = user.UserId,
                    UserType = user.UserType ?? 0,
                    RequiresMpinSetup = user.PIN == null,
                    RequiresOtp = true
                });
            }

            return StatusCode(500, new { Message = "Failed to send OTP." });
        }

        [ApiExplorerSettings(IgnoreApi = true)] // Exclude from Swagger
        public async Task<string> SMSSender(string mobile, string message)
        {
            string webURL = "https://app.sms8.io/services/send.php?key=";
            string apiKey = "f5bb220e887e2f007915ea751fdcfe82c0853c24";
            string device = "3644&type=sms&useRandomDevice=1&prioritize=1";
            string finUrl = $"{webURL}{apiKey}&number={mobile}&message={Uri.EscapeDataString(message)}&devices={device}";

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(finUrl, null);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        // TODO: Parse result to confirm success
                        return result;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Log error (e.g., using ILogger)
                return null;
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == dto.MobileNumber);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (user.Otp == dto.Otp && user.OtpCreatedAt.HasValue &&
                (DateTime.UtcNow - user.OtpCreatedAt.Value).TotalMinutes <= 5)
            {
                user.LastOtpVerification = DateTime.UtcNow;
                user.Otp = null;
                user.OtpCreatedAt = null;
                await _context.SaveChangesAsync();

                return Ok(new LoginResponseDto
                {
                    Message = user.PIN == null ? "OTP verified. Please create MPIN." : "OTP verified.",
                    UserId = user.UserId,
                    UserType = user.UserType ?? 0,
                    RequiresMpinSetup = user.PIN == null,
                    RequiresOtp = false
                });
            }

            return BadRequest(new { Message = "Invalid or expired OTP." });
        }

        [HttpPost("create-mpin")]
        public async Task<IActionResult> CreateMpin([FromBody] CreateMpinRequestDto dto)
        {
            // TODO: Implement MPIN creation logic
            // 1. Find user by mobile number (or UserId if passed after OTP verification)
            // 2. Ensure MPIN meets complexity requirements (e.g., 4 or 6 digits)
            // 3. Hash and store the MPIN securely
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == dto.MobileNumber);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

           // Validate MPIN (4 digits)
           if (!dto.Mpin.HasValue || dto.Mpin.Value < 1000 || dto.Mpin.Value > 9999)
            {
                return BadRequest(new { Message = "MPIN must be a 4-digit number." });
            }

            // TODO: Hash the MPIN in production
            user.PIN = dto.Mpin; // Store as int for now
            await _context.SaveChangesAsync();
            // In a real app, hash this PIN before saving

            return Ok(new LoginResponseDto
            {
                Message = "MPIN created successfully.",
                UserId = user.UserId,
                UserType = user.UserType ?? 0,
                RequiresMpinSetup = false,
                RequiresOtp = false
            });
         }

        [HttpPost("mpin-login")]
        public async Task<IActionResult> MpinLogin([FromBody] MpinLoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == dto.MobileNumber);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

                    // Validate and compare MPIN
            if (user.PIN != dto.Mpin)
            {
                return BadRequest(new { Message = "Invalid MPIN." });
            }

            
                return Ok(new LoginResponseDto
                {
                    Message = "MPIN login successful.",
                    UserId = user.UserId,
                    UserType = user.UserType ?? 0,
                    RequiresMpinSetup = false,
                    RequiresOtp = false
                });
            
           
        }

        // TODO: Add helper method for JWT token generation if needed
        // private string GenerateJwtToken(tblUser user) { ... }
    }
}