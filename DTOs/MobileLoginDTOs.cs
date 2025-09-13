using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.DTOs
{
    public class MobileLoginRequestDto
    {
        [Required]
        [Phone]
        public string? MobileNumber { get; set; }
    }

    public class VerifyOtpRequestDto
    {
        [Required]
        [Phone]
        public string? MobileNumber { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 4)] // Assuming OTP can be 4 to 6 digits
        public string? Otp { get; set; }
    }

    public class CreateMpinRequestDto
    {
        [Required]
        [Phone]
        public string? MobileNumber { get; set; } // Or UserId if preferred after OTP verification

        [Required]
        // Basic validation, consider more robust validation like regex for exactly 4 or 6 digits
        [Range(1000, 999999)] // Assuming MPIN is a number, adjust if it's a string
        public int? Mpin { get; set; }
    }

    public class MpinLoginRequestDto
    {
        [Required]
        [Phone]
        public string? MobileNumber { get; set; }

        [Required]
        [Range(1000, 999999)] // Assuming MPIN is a number
        public int? Mpin { get; set; }
    }
}
public class LoginResponseDto
{
    public string Message { get; set; }
    public int UserId { get; set; }
    public int UserType { get; set; }
    public bool RequiresMpinSetup { get; set; }
    public bool RequiresOtp { get; set; }
    }
