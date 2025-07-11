using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.DTOs
{
    public class StatementRequestDto
    {
        [Required]
        public int UserId { get; set; }
        
        public int? LoanId { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public string TransactionType { get; set; } = "all"; // all, payment, interest, fee, adjustment
        
        public decimal MinAmount { get; set; } = 0m; // Use decimal instead of double
        
        public decimal MaxAmount { get; set; } = 999999m; // Use decimal instead of double
    }

    public class AccountInfoDto
    {
        public string AccountNumber { get; set; } = "";
        public string AccountStatus { get; set; } = "";
        public string ClientName { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string LoanDescription { get; set; } = "";
        public int LoanId { get; set; }
    }

    public class AccountSummaryDto
    {
        public decimal OpeningBalance { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal InterestCharges { get; set; }
        public decimal FeesApplied { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class TransactionDto
    {
        public string Id { get; set; } = "";
        public string Date { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal RunningBalance { get; set; }
        public string Type { get; set; } = "";
        public string? PaymentMethod { get; set; }
        public string? Reference { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
    }

    public class StatementResponseDto
    {
        public List<AccountInfoDto> AccountInfo { get; set; } = new();
        public AccountSummaryDto AccountSummary { get; set; } = new();
        public List<TransactionDto> Transactions { get; set; } = new();
    }
}
