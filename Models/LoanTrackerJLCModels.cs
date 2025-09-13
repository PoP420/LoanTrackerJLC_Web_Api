using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.Models
{

    public class Response
    {
        public int? Code { get; set; }
        public string? Message { get; set; }
    }
    public class userMdl
    {
        public string? Password { get; set; }
        public int? UserId { get; set; } // Changed from string? to int?
        public string? UserName { get; set; }
        public int? UserType { get; set; }
        public string? PIN { get; set; }
        public string? FullName { get; set; }

    }
    public class userTypeMdl
    {
        public int? UserTypeId { get; set; }
        public string? UserType { get; set; }

    }

    

    public class PaymentMdl
    {
        public int? PayID { get; set; }

        public int? TrxnID { get; set; }
        public int? LoanID { get; set; }
        public float? Amount { get; set; }
        public DateTime? DueDate { get; set; }

    }

    public class AccountMdl
    {
        public int? PId { get; set; }
        public string? UserId { get; set; }
        public int? LoanId { get; set; }
        public string? lastname { get; set; }
        public string? firstname { get; set; }
        public string? address1 { get; set; }
        public string? ClientFullName { get; set; } // Added for sp_GetUnassignedLoans
        public string? UserName { get; set; }
        public float? Amount { get; set; }
        public float? Interest { get; set; }
        public float? Total { get; set; }
        public int? LoanTerm { get; set; }
        public string? LoanDescription { get; set; }
        public byte[]? img { get; set; }
        public byte[]? IDPicture { get; set; }
        public string? Status { get; set; } // New property for loan status

    }
    public class LoanTrxnMdl
    {
        public int? Id { get; set; }
        public int? LoanId { get; set; }
        public string? LoanDescription { get; set; }
        public float? PrincipalDue { get; set; }
        public float? InterestDue { get; set; }
        public float? PenaltiesDue { get; set; }
        public float? ServiceFeesDue { get; set; } // Added for new service fees
        public float? TotalDue { get; set; }
        public DateTime? DueDate { get; set; }
        public byte[]? img { get; set; }
        public string? Remarks { get; set; }
        public bool? isPaid { get; set; }
        public string? lastname { get; set; }
        public string? firstname { get; set; }
        public float? CalculatedPenaltyForDisplay { get; set; }
        public float? TotalDueWithCalcPenaltyForDisplay { get; set; }
    }

    public class SmsMsgMdl
    {
        public int? Id { get; set; }

        public string? Message { get; set; }

    }

    public class LoanTypeMdl
    {
        public int? LoanTypeId { get; set; }
        public string? LoanDescription { get; set; }
        public float? InterestRate { get; set; }
        public float? PenaltiesRate { get; set; }
        public float? Term { get; set; }

    }
    public class LoanTermMdl
    {
        public int? TermId { get; set; }
        public string? TermDescription { get; set; }
        public int? TermCount { get; set; }
    }

    public class LoanSummary
    {
        public int? CTR { get; set; }
        public string? LoanDescription { get; set; }
        public string? LoanStatus { get; set; }
        public string? Month { get; set; }
        public string? TotalDue { get; set; }
        public string? DueDate { get; set; }
        public float? InterestDue { get; set; }
        public float? PenaltiesDue { get; set; }
        public float? PrincipalDue { get; set; }
        public string? lastname { get; set; }
        public string? firstname { get; set; }
        public string? address1 { get; set; }
        public string? UserName { get; set; }
        public string? MobileNo { get; set; } // Added for SMS

    }
    public class TrxnHistory
    {
        public int? LoanID { get; set; }
        public int? TrxnID { get; set; }
        public float? Amount { get; set; }
        public string? Remarks { get; set; }
        public string? PDate { get; set; }
        public float? NewAmountOnFee { get; set; }

    }

    public class SMSMdl
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Recipients { get; set; }
        public string? Message { get; set; }
        public string? ApiCode { get; set; }
        public string? SenderId { get; set; }


    }

    public class MsgMdl
    {
        public string? Mobile { get; set; }
        public string? Message { get; set; }
    }


    public class PrintValue
    {
        public string? Code { get; set; }
        public string? Value { get; set; }
        public string? FileType { get; set; }
        public byte[]? ActualSize { get; set; }
        public string? Description { get; set; }
        public DateTime? DateModified { get; set; }


    }

    public class AccountAssignment
    {
        public int AssignmentID { get; set; } // Primary Key
        public int LoanId { get; set; } // Foreign Key to tblLoan.LoanId
        public int CollectorUserID { get; set; } // Foreign Key to tblUser.UserId (for the collector)
        public int AssignedByUserID { get; set; } // Foreign Key to tblUser.UserId (for the admin/clerk)
        public DateTime AssignmentTimestamp { get; set; }
        public string? Status { get; set; } // e.g., "Assigned", "In Progress", "Collected", "Escalated"
                                           // Consider using an enum for better type safety and consistency
        public string? Notes { get; set; }

        // Optional: Navigation properties if you plan to use an ORM like Entity Framework Core
        // public virtual AccountMdl Loan { get; set; } // Assuming AccountMdl maps to tblLoan
        // public virtual userMdl CollectorUser { get; set; } // Assuming userMdl maps to tblUser
        // public virtual userMdl AssignedByUser { get; set; } // Assuming userMdl maps to tblUser
    }
    public class AccountAssignmentViewMdl
    {
        public int AssignmentID { get; set; }
        public int LoanId { get; set; }
        public string? ClientFullName { get; set; } // From tblUser (via tblLoan.UserId)
        public decimal? LoanAmount { get; set; } // From tblLoan
        public string? CollectorFullName { get; set; } // From tblUser (CollectorUserID)
        public string? AssignedByFullName { get; set; } // From tblUser (AssignedByUserID)
        public DateTime AssignmentTimestamp { get; set; }
        public string? Status { get; set; } // Assignment Status
        public string? Notes { get; set; }
        public string? LoanStatus { get; set; } // From tblLoan
        public int? LoanTypeID { get; set; } // Added to link to LoanTypeMdl for PenaltiesRate
        public List<LoanTrxnMdl>? RepaymentSchedule { get; set; } // To hold loan transactions
    }




    // ... other models like tblPerson ...
    public class tblPerson
    {
        [Key]
        public int PId { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(150)]
        public string? address1 { get; set; }       
        public int? UID { get; set; } // Foreign key to tblUser.UserId
        public tblUser? User { get; set; } // Optional bidirectional navigation
    }
}
