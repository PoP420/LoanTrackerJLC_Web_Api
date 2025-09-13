using Microsoft.AspNetCore.Mvc;
using LoanTrackerJLC.Models;
using LoanTrackerJLC.DTOs;
using Microsoft.EntityFrameworkCore;
using LoanTrackerJLC.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanTrackerJLC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly LoanTrackerJLCDbContext _context;

        public ProfileController(LoanTrackerJLCDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile(int userId, int userType)
        {
            var user = await _context.tblUsers
                .Include(u => u.Person)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.UserType == userType);
            if (user == null) return NotFound();

            var image = await _context.tblImages
                .Where(i => i.UID == userId && i.loanID == null)
                .Select(i => i.img)
                .FirstOrDefaultAsync();

            var profile = new
            {
                name = user.FullName,
                firstName = user.Person?.FirstName,
                lastName = user.Person?.LastName,
                phone = user.UserName,
                address = user.Person?.address1,
                accountNumber = userType == 1 ? $"LA-2024-00{userId}" : $"COL-2024-00{userId}",
                accountStatus = "active",
                memberSince = "2023-01-15",
                avatarUrl = image != null ? Convert.ToBase64String(image) : null
            };

            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] dynamic changes)
        {
            int userId = changes.userId;
            int userType = changes.userType;
            string firstName = changes.firstName;
            string lastName = changes.lastName;
            string phone = changes.phone;
            string address = changes.address;

            var user = await _context.tblUsers
                .Include(u => u.Person)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.UserType == userType);
            if (user == null) return NotFound();

            user.FullName = $"{firstName} {lastName}";
            user.UserName = phone;

            if (user.Person != null)
            {
                user.Person.FirstName = firstName;
                user.Person.LastName = lastName;
                if (userType == 1) user.Person.address1 = address;
            }

            await _context.SaveChangesAsync();
            return Ok(new Response { Code = 200, Message = "Profile updated" });
        }

        [HttpPut("mpin")]
        public async Task<IActionResult> ChangeMPIN(int userId, [FromBody] string newPIN)
        {
            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(newPIN))
                return BadRequest(new { Message = "MPIN cannot be empty." });

            if (newPIN.Length != 4 || !int.TryParse(newPIN, out int pinValue))
                return BadRequest(new { Message = "MPIN must be exactly 4 digits." });

            user.PIN = pinValue;
            await _context.SaveChangesAsync();
            return Ok(new Response { Code = 200, Message = "MPIN updated" });
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar(int userId, [FromBody] string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
                return BadRequest("Invalid image data");

            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return NotFound();

            byte[] imageBytes;
            try
            {
                // Remove data URI prefix if present (e.g., "data:image/jpeg;base64,")
                string cleanBase64 = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;
                imageBytes = Convert.FromBase64String(cleanBase64);
            }
            catch (FormatException)
            {
                return BadRequest("Invalid base64 image format");
            }

            var existingImage = await _context.tblImages
                .FirstOrDefaultAsync(i => i.UID == userId && i.loanID == null);

            if (existingImage != null)
            {
                existingImage.img = imageBytes;
                existingImage.userID = userId;
                existingImage.UID = userId;
            }
            else
            {
                var newImage = new tblImage
                {
                    UID = userId,
                    userID = userId,
                    loanID = null,
                    img = imageBytes
                };
                _context.tblImages.Add(newImage);
            }

            await _context.SaveChangesAsync();
            return Ok(new Response { Code = 200, Message = "Avatar updated" });
        }
    }
}