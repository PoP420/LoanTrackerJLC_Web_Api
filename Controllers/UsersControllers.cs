using LoanTrackerJLC.Data;
using LoanTrackerJLC.DTOs;
using LoanTrackerJLC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LoanTrackerJLC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly LoanTrackerJLCDbContext _context;

        public UsersController(LoanTrackerJLCDbContext context)
        {
            _context = context;
        }

        // GET: api/users/{userId}/image
        [HttpGet("users/{userId}/image")]
        public async Task<IActionResult> GetUserImage(int userId)
        {
            var image = await _context.tblImages.FirstOrDefaultAsync(i => i.userID == userId);
            if (image == null)
            {
                return NotFound(new { Message = "Image not found for this user." });
            }

            return File(image.img, "image/jpeg"); // Assuming the image is in JPEG format
        }
    }
}

