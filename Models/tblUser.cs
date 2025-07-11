using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.Models
{
    public class tblUser
    {
        [Key]
        public int UserId { get; set; }

        [StringLength(50)]
        public string? UserName { get; set; }

        [StringLength(50)]
        public string? Password { get; set; }

        public int? PIN { get; set; }

        public int? UserType { get; set; }

        [StringLength(50)]
        public string? FullName { get; set; }

        // Navigation property to tblPerson
        public tblPerson? Person { get; set; }
    }
}