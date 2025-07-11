using LoanTrackerJLC.Models;

namespace LoanTrackerJLC.DTOs
{
    public class AssignedLoanDto
    {
        public tblLoan Loan { get; set; }
        public tblUser Client { get; set; }
        public tblImage Image { get; set; }
    }
}