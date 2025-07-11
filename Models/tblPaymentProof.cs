using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanTrackerJLC.Models
{
    [Table("tblPaymentProofs")]
    public class tblPaymentProof
    {
        [Key]
        public int ProofID { get; set; }

        [Required]
        public int PaymentHistoryID { get; set; } // Foreign key to tblPaymentHistory

        [Required]
        public byte[] ImageData { get; set; }

        [StringLength(100)]
        public string? FileType { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }

        public DateTime UploadedTimestamp { get; set; }

        // Navigation property
        [ForeignKey("PaymentHistoryID")]
        public virtual tblPaymentHistory? PaymentHistory { get; set; }
    }
}