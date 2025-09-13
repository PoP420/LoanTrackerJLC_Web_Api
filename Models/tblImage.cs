using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LoanTrackerJLC.Models
{
    [Table("tblImages")]
    public class tblImage
    {
        [Key]
        public int imgID { get; set; }

        public int? loanID { get; set; } // Foreign key to tblLoan

        public int? userID { get; set; } // User who uploaded or is associated with the image

        public byte[]? img { get; set; } // Image data

        public int? UID { get; set; } // Potentially to link to tblPaymentHistory.Id or other unique identifier

        // Navigation properties (optional, but good practice)
        [ForeignKey("loanID")]
        [JsonIgnore]
        public virtual tblLoan? Loan { get; set; }

        [ForeignKey("userID")]
        [JsonIgnore]
        public virtual tblUser? User { get; set; }
    }
}