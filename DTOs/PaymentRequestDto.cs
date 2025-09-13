using System;
using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.DTOs
{
    public class PaymentRequestDto
    {
        public int LoanID { get; set; }
        public int UserID { get; set; }
        public int LoanTransactionID { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        [StringLength(100, ErrorMessage = "GCash Reference Number must be between 10 and 100 characters.", MinimumLength = 10)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "GCash Reference Number must contain only alphanumeric characters.")]
        public string? GCashReferenceNo { get; set; }
    }
}