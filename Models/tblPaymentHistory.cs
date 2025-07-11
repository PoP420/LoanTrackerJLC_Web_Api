using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanTrackerJLC.Models
{
    public class tblPaymentHistory
    {
          [Key]
        public int Id { get; set; }

        public int? TrxnID { get; set; }

        public int? LoanID { get; set; }

        [Column(TypeName = "money")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "date")]
        public DateTime? PDate { get; set; }

        public int? userID { get; set; }

        // NEW: Approval workflow fields (added via migration)
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [Column(TypeName = "datetime2")]
        public DateTime? SubmittedDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime? ApprovedDate { get; set; }

        [MaxLength(100)]
        public string? ApprovedBy { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        public string? ApprovalNotes { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }
    }
}