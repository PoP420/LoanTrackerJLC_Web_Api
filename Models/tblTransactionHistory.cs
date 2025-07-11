using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanTrackerJLC.Models
{
    public class tblTransactionHistory
    {
        [Key]
        public int Id { get; set; }

        public int? TrxnID { get; set; }

        public int? LoanID { get; set; }

        [Column(TypeName = "money")]
        public decimal? Amount { get; set; }

        public DateTime? PDate { get; set; }

        public int? userID { get; set; }

        [StringLength(50)]
        public string? Remarks { get; set; }

        [Column(TypeName = "money")]
        public decimal NewAmountOnFee { get; set; } // As per user feedback, this is NOT NULL
    }
}