using LoanTrackerJLC.Data;
using LoanTrackerJLC.DTOs;
using LoanTrackerJLC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;


using System.Threading.Tasks;

namespace LoanTrackerJLC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatementController : ControllerBase
    {
        private readonly LoanTrackerJLCDbContext _context;

        public StatementController(LoanTrackerJLCDbContext context)
        {
            _context = context;
        }

        // GET: api/statement/account-statement/{userId}/{loanId}
        [HttpGet("account-statement/{userId}/{loanId}")]
        public async Task<IActionResult> GetAccountStatement(int userId, int loanId)
        {
            try
            {
                // Validate inputs
                if (userId <= 0 || loanId <= 0)
                {
                    return BadRequest(new { Message = "Invalid UserId or LoanId." });
                }

                // User and Loan Details (including Loan Type)
                var accountInfo = await (from u in _context.tblUsers
                                        join l in _context.tblLoans on u.UserId equals l.UserId
                                        join lt in _context.tblLoanTypes on l.LoanTypeID equals lt.LoanTypeID
                                        where u.UserId == userId && l.LoanId == loanId
                                        select new
                                        {
                                            UserId = u.UserId,
                                            FullName = u.FullName,
                                            UserName = u.UserName,
                                            LoanId = l.LoanId,
                                            LoanTypeID = l.LoanTypeID,
                                            LoanDescription = lt.LoanDescription,
                                            InterestRate = lt.InterestRate,
                                            PenaltiesRate = lt.PenaltiesRate,
                                            MaxTerm = lt.MaxTerm,
                                            LoanAmount = l.Amount,
                                            Interest = l.Interest,
                                            TotalLoanAmount = l.Total,
                                            LoanTerm = l.LoanTerm,
                                            LoanStatus = l.Status,
                                            LoanCreatedDate = l.CreatedDate
                                        }).FirstOrDefaultAsync();

                if (accountInfo == null)
                {
                    return NotFound(new { Message = "No account found for the specified UserId and LoanId." });
                }

                // Outstanding Balance from tblLoanTransactions
                var outstandingBalances = await (from ltr in _context.tblLoanTransactions
                                                where ltr.LoanID == loanId && ltr.isPaid == false
                                                select new
                                                {
                                                    ltr.Id,
                                                    ltr.LoanID,
                                                    ltr.PrincipalDue,
                                                    ltr.InterestDue,
                                                    ltr.PenaltiesDue,
                                                    ltr.ServiceFeesDue,
                                                    ltr.TotalDue,
                                                    ltr.DueDate,
                                                    ltr.isPaid,
                                                    ltr.Remarks,
                                                    ltr.WeeklyDue,
                                                    ltr.PenaltyMonthsApplied,
                                                    ltr.MonthlyDueDate
                                                })
                                                .OrderBy(ltr => ltr.DueDate)
                                                .ToListAsync();

                var totalOutstandingBalance = outstandingBalances.Sum(ltr => ltr.TotalDue);

                // Payment History
                var paymentHistory = await (from ph in _context.tblPaymentHistories
                                           where ph.LoanID == loanId && ph.userID == userId
                                           select new
                                           {
                                               ph.Id,
                                               ph.TrxnID,
                                               ph.LoanID,
                                               PaymentAmount = ph.Amount,
                                               PaymentDate = ph.PDate,
                                               PaymentStatus = ph.Status,
                                               ph.SubmittedDate,
                                               ph.ApprovedDate,
                                               ph.ApprovedBy,
                                               ph.RejectionReason,
                                               ph.ApprovalNotes,
                                               PaymentRemarks = ph.Remarks
                                           })
                                           .OrderByDescending(ph => ph.PaymentDate)
                                           .ToListAsync();

                // Transaction History
                var transactionHistory = await (from th in _context.tblTransactionHistories
                                               where th.LoanID == loanId && th.userID == userId
                                               select new
                                               {
                                                   th.Id,
                                                   th.TrxnID,
                                                   th.LoanID,
                                                   TransactionAmount = th.Amount,
                                                   TransactionDate = th.PDate,
                                                   TransactionRemarks = th.Remarks,
                                                   th.NewAmountOnFee
                                               })
                                               .OrderByDescending(th => th.TransactionDate)
                                               .ToListAsync();

                // Summary of Totals
                var summary = await (from l in _context.tblLoans
                                     join lt in _context.tblLoanTypes on l.LoanTypeID equals lt.LoanTypeID
                                     from ltr in _context.tblLoanTransactions
                                         .Where(ltr => ltr.LoanID == l.LoanId && ltr.isPaid == false)
                                         .DefaultIfEmpty()
                                     from ph in _context.tblPaymentHistories
                                         .Where(ph => ph.LoanID == l.LoanId && ph.Status == "Approved")
                                         .DefaultIfEmpty()
                                     where l.LoanId == loanId && l.UserId == userId
                                     group new { l, lt, ltr, ph } by new
                                     {
                                         l.LoanId,
                                         lt.LoanDescription,
                                         l.Amount,
                                         l.Interest,
                                         l.Total
                                     } into g
                                     select new
                                     {
                                         g.Key.LoanId,
                                         g.Key.LoanDescription,
                                         OriginalLoanAmount = g.Key.Amount,
                                         TotalInterest = g.Key.Interest,
                                         TotalLoanAmount = g.Key.Total,
                                         TotalOutstandingBalance = g.Sum(x => x.ltr != null ? x.ltr.TotalDue : 0),
                                         TotalPaymentsMade = g.Sum(x => x.ph != null ? x.ph.Amount : 0)
                                     }).FirstOrDefaultAsync();

                // Combine all sections into a single response
                var response = new
                {
                    AccountInfo = accountInfo,
                    OutstandingBalances = outstandingBalances.Select(ob => new
                    {
                        ob.Id,
                        ob.LoanID,
                        ob.PrincipalDue,
                        ob.InterestDue,
                        ob.PenaltiesDue,
                        ob.ServiceFeesDue,
                        ob.TotalDue,
                        ob.DueDate,
                        ob.isPaid,
                        ob.Remarks,
                        ob.WeeklyDue,
                        ob.PenaltyMonthsApplied,
                        ob.MonthlyDueDate,
                        TotalOutstandingBalance = totalOutstandingBalance
                    }),
                    PaymentHistory = paymentHistory,
                    TransactionHistory = transactionHistory,
                    Summary = summary
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving account statement", Error = ex.Message });
            }
        }

        // POST: api/statement/account-summary
        [HttpPost("account-summary")]
        public async Task<IActionResult> GetAccountSummary([FromBody] StatementRequestDto request)
        {
            try
            {
                var loan = await _context.tblLoans.FirstOrDefaultAsync(l => l.LoanId == request.LoanId);
                if (loan == null)
                {
                    return NotFound(new { Message = "Loan not found." });
                }

                // Calculate summary data - Fixed to handle nullable decimals properly
                var openingBalance = loan.Amount ?? 0m;

                var totalPayments = await _context.tblPaymentHistories
                    .Where(ph => ph.LoanID == request.LoanId 
                        && ph.Status == "Approved" 
                        && ph.ApprovedDate >= request.StartDate 
                        && ph.ApprovedDate <= request.EndDate)
                    .SumAsync(ph => ph.Amount ?? 0m);

                // Fixed: Handle nullable decimals properly for tblLoanTransaction
                var interestCharges = await _context.tblLoanTransactions
                    .Where(lt => lt.LoanID == request.LoanId 
                        && lt.DueDate >= request.StartDate 
                        && lt.DueDate <= request.EndDate)
                    .SumAsync(lt => lt.InterestDue ?? 0m);

                // Fixed: ServiceFeesDue is non-nullable, PenaltiesDue is nullable
                var feesApplied = await _context.tblLoanTransactions
                    .Where(lt => lt.LoanID == request.LoanId 
                        && lt.DueDate >= request.StartDate 
                        && lt.DueDate <= request.EndDate)
                    .SumAsync(lt => (lt.PenaltiesDue ?? 0m) + lt.ServiceFeesDue);

                var currentBalance = await _context.tblLoanTransactions
                    .Where(lt => lt.LoanID == request.LoanId && lt.isPaid == false)
                    .SumAsync(lt => lt.TotalDue ?? 0m);

                var summary = new
                {
                    OpeningBalance = openingBalance,
                    TotalPayments = totalPayments,
                    InterestCharges = interestCharges,
                    FeesApplied = feesApplied,
                    CurrentBalance = currentBalance
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error calculating account summary", Error = ex.Message });
            }
        }

        // POST: api/statement/transactions
        [HttpPost("transactions")]
        public async Task<IActionResult> GetTransactions([FromBody] StatementRequestDto request)
        {
            try
            {
                var transactions = new List<object>();

                // Get payment transactions
                var payments = await _context.tblPaymentHistories
                    .Where(ph => ph.LoanID == request.LoanId 
                        && ph.Status == "Approved" 
                        && ph.ApprovedDate >= request.StartDate 
                        && ph.ApprovedDate <= request.EndDate
                        && (request.TransactionType == "all" || request.TransactionType == "payment")
                        && ph.Amount >= request.MinAmount 
                        && ph.Amount <= request.MaxAmount)
                    .Select(ph => new
                    {
                        Id = $"PAY-{ph.Id}",
                        Date = ph.ApprovedDate != null ? ph.ApprovedDate.Value.ToString("yyyy-MM-dd") : 
                               ph.PDate != null ? ph.PDate.Value.ToString("yyyy-MM-dd") : "",
                        Description = "Payment Received",
                        DebitAmount = 0.0m,
                        CreditAmount = ph.Amount ?? 0m,
                        RunningBalance = 0.0m, // Will calculate later
                        Type = "payment",
                        PaymentMethod = "Online", // You can enhance this
                        Reference = $"PAY-{ph.Id}",
                        PrincipalAmount = (ph.Amount ?? 0m) * 0.8m, // Assuming 80% principal
                        InterestAmount = (ph.Amount ?? 0m) * 0.2m,  // Assuming 20% interest
                        TransactionDate = ph.ApprovedDate ?? ph.PDate ?? DateTime.MinValue
                    })
                    .ToListAsync();

                transactions.AddRange(payments);

                // Get interest charges
                if (request.TransactionType == "all" || request.TransactionType == "interest")
                {
                    var interestCharges = await _context.tblLoanTransactions
                        .Where(lt => lt.LoanID == request.LoanId 
                            && lt.DueDate >= request.StartDate 
                            && lt.DueDate <= request.EndDate
                            && lt.InterestDue > 0
                            && lt.InterestDue >= request.MinAmount 
                            && lt.InterestDue <= request.MaxAmount)
                        .Select(lt => new
                        {
                            Id = $"INT-{lt.Id}",
                            Date = lt.DueDate != null ? lt.DueDate.Value.ToString("yyyy-MM-dd") : "",
                            Description = "Interest Charge",
                            DebitAmount = lt.InterestDue ?? 0m,
                            CreditAmount = 0.0m,
                            RunningBalance = 0.0m,
                            Type = "interest",
                            PaymentMethod = (string)null,
                            Reference = $"INT-{lt.Id}",
                            PrincipalAmount = 0.0m,
                            InterestAmount = lt.InterestDue ?? 0m,
                            TransactionDate = lt.DueDate ?? DateTime.MinValue
                        })
                        .ToListAsync();

                    transactions.AddRange(interestCharges);
                }

                // Get fee charges - Fixed to handle ServiceFeesDue as non-nullable
                if (request.TransactionType == "all" || request.TransactionType == "fee")
                {
                    var feeCharges = await _context.tblLoanTransactions
                        .Where(lt => lt.LoanID == request.LoanId 
                            && lt.DueDate >= request.StartDate 
                            && lt.DueDate <= request.EndDate
                            && ((lt.PenaltiesDue ?? 0m) + lt.ServiceFeesDue) > 0
                            && ((lt.PenaltiesDue ?? 0m) + lt.ServiceFeesDue) >= request.MinAmount 
                            && ((lt.PenaltiesDue ?? 0m) + lt.ServiceFeesDue) <= request.MaxAmount)
                        .Select(lt => new
                        {
                            Id = $"FEE-{lt.Id}",
                            Date = lt.DueDate != null ? lt.DueDate.Value.ToString("yyyy-MM-dd") : "",
                            Description = (lt.PenaltiesDue ?? 0m) > 0 ? "Late Payment Fee" : "Service Fee",
                            DebitAmount = (lt.PenaltiesDue ?? 0m) + lt.ServiceFeesDue,
                            CreditAmount = 0.0m,
                            RunningBalance = 0.0m,
                            Type = "fee",
                            PaymentMethod = (string)null,
                            Reference = $"FEE-{lt.Id}",
                            PrincipalAmount = 0.0m,
                            InterestAmount = 0.0m,
                            TransactionDate = lt.DueDate ?? DateTime.MinValue
                        })
                        .ToListAsync();

                    transactions.AddRange(feeCharges);
                }

                // Sort by date descending and calculate running balance
                var sortedTransactions = transactions
                    .OrderByDescending(t => GetTransactionDate(t))
                    .ToList();

                // Calculate running balances
                var currentBalance = await _context.tblLoanTransactions
                    .Where(lt => lt.LoanID == request.LoanId && lt.isPaid == false)
                    .SumAsync(lt => lt.TotalDue ?? 0m);

                var runningBalance = currentBalance;
                var transactionsWithBalance = new List<object>();

                foreach (var transaction in sortedTransactions)
                {
                    var debitAmount = GetPropertyValue<decimal>(transaction, "DebitAmount");
                    var creditAmount = GetPropertyValue<decimal>(transaction, "CreditAmount");

                    var transactionWithBalance = new
                    {
                        Id = GetPropertyValue<string>(transaction, "Id"),
                        Date = GetPropertyValue<string>(transaction, "Date"),
                        Description = GetPropertyValue<string>(transaction, "Description"),
                        DebitAmount = debitAmount,
                        CreditAmount = creditAmount,
                        RunningBalance = runningBalance,
                        Type = GetPropertyValue<string>(transaction, "Type"),
                        PaymentMethod = GetPropertyValue<string>(transaction, "PaymentMethod"),
                        Reference = GetPropertyValue<string>(transaction, "Reference"),
                        PrincipalAmount = GetPropertyValue<decimal>(transaction, "PrincipalAmount"),
                        InterestAmount = GetPropertyValue<decimal>(transaction, "InterestAmount")
                    };

                    transactionsWithBalance.Add(transactionWithBalance);

                    // Update running balance for next iteration
                    runningBalance += debitAmount - creditAmount;
                }

                return Ok(transactionsWithBalance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving transactions", Error = ex.Message });
            }
        }

        // Helper methods to handle dynamic object property access
        private DateTime GetTransactionDate(object transaction)
        {
            try
            {
                var property = transaction.GetType().GetProperty("TransactionDate");
                if (property != null)
                {
                    var value = property.GetValue(transaction);
                    if (value is DateTime dateTime)
                    {
                        return dateTime;
                    }
                }
                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }
                }
                return default(T);
            }
            catch
            {
                return default(T);
            }
        }
    }
}