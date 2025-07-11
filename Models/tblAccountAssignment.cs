using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanTrackerJLC.Models
{
    public class tblAccountAssignment
    {
        [Key]
        public int AssignmentID { get; set; }

        public int LoanId { get; set; }

        public int CollectorUserID { get; set; }

        public int AssignedByUserID { get; set; }

        public DateTime AssignmentTimestamp { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("LoanId")]
        public virtual tblLoan? Loan { get; set; }

        [ForeignKey("CollectorUserID")]
        public virtual tblUser? CollectorUser { get; set; }

        [ForeignKey("AssignedByUserID")]
        public virtual tblUser? AssignedByUser { get; set; }
    }
}