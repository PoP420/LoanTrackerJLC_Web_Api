using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.DTOs
{
    public class CollectorPaymentDto
    {
        [Required]
        public int LoanId { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string? Notes { get; set; }
    }
}
