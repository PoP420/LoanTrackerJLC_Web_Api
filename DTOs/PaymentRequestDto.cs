using System;

namespace LoanTrackerJLC.DTOs
{
    public class PaymentRequestDto
    {
        public int LoanID { get; set; }
        public int UserID { get; set; }
        public int LoanTransactionID { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}