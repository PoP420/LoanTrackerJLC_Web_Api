using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanTrackerJLC.Models
{
    [Table("tblLoanTransactions")]
    public class tblLoanTransaction
    {
        [Key]
        public int Id { get; set; }

        public int? LoanID { get; set; }

        [Column(TypeName = "money")]
        public decimal? PrincipalDue { get; set; }

        [Column(TypeName = "money")]
        public decimal? InterestDue { get; set; }

        [Column(TypeName = "money")]
        public decimal? PenaltiesDue { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalDue { get; set; }

        public decimal? PaidAmount { get; set; }

        public DateTime? DueDate { get; set; }

        public bool? isPaid { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        [Column(TypeName = "money")]
        public decimal? WeeklyDue { get; set; }

        public int PenaltyMonthsApplied { get; set; }

        public DateTime? MonthlyDueDate { get; set; }

        [Column(TypeName = "money")]
        public decimal ServiceFeesDue { get; set; }




    }
}