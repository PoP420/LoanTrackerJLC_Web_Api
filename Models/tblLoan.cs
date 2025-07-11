using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanTrackerJLC.Models
{
    public class tblLoan
    {
        [Key]
        public int LoanId { get; set; }

        public int? UserId { get; set; }

        public int? LoanTypeID { get; set; }

        [Column(TypeName = "money")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "money")]
        public decimal? Interest { get; set; }

        [Column(TypeName = "money")]
        public decimal? Total { get; set; }

        public int? LoanTerm { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [ForeignKey("UserId")]
        public virtual tblUser? User { get; set; }
    }
}