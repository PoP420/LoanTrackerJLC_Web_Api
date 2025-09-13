using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.Models
{
    public class tblUser
    {
        [Key]
        public int UserId { get; set; }

        [StringLength(50)]
        public string? UserName { get; set; } // Used for mobile number

        [StringLength(50)]
        public string? Password { get; set; }

        public int? PIN { get; set; } // MPIN

        public int? UserType { get; set; }

        [StringLength(50)]
        public string? FullName { get; set; }

        [StringLength(6)]
        public string? Otp { get; set; } // Added for OTP storage

        public DateTime? OtpCreatedAt { get; set; } // Added for OTP creation timestamp

        public DateTime? LastOtpVerification { get; set; } // Added for last OTP verification

        // Navigation property to tblPerson
        public tblPerson? Person { get; set; }


       
    }
}