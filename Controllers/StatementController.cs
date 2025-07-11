using LoanTrackerJLC.Data;
using LoanTrackerJLC.DTOs;
using LoanTrackerJLC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/statement/account-info/{userId}
        [HttpGet("account-info/{userId}")]
        public async Task<IActionResult> GetAccountInfo(int userId)
        {
            try
            {
                var accountInfo = await (from u in _context.tblUsers
                    join l in _context.tblLoans on u.UserId equals l.UserId
                    where u.UserId == userId && (l.Status == "Active" || l.Status == "Overdue" || l.Status == "Current")
                    select new
                    {
                        AccountNumber = $"LA-{l.LoanId:D6}",
                        AccountStatus = l.Status,
                        ClientName = u.FullName,
                        LastUpdated = DateTime.UtcNow,
                        LoanAmount = l.Amount,
                        TotalAmount = l.Total,
                        LoanDescription = "Personal Loan", // You can join with LoanType if needed
                        LoanId = l.LoanId
                    }).ToListAsync();

                if (!accountInfo.Any())
                {
                    return NotFound(new { Message = "No active accounts found for this user." });
                }

                return Ok(accountInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving account info", Error = ex.Message });
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

        // POST: api/statement/full-statement
        [HttpPost("full-statement")]
        public async Task<IActionResult> GetFullStatement([FromBody] StatementRequestDto request)
        {
            try
            {
                // Get account info
                var accountInfoResponse = await GetAccountInfo(request.UserId);
                if (accountInfoResponse is not OkObjectResult accountInfoOk)
                {
                    return accountInfoResponse;
                }

                // Get account summary
                var summaryResponse = await GetAccountSummary(request);
                if (summaryResponse is not OkObjectResult summaryOk)
                {
                    return summaryResponse;
                }

                // Get transactions
                var transactionsResponse = await GetTransactions(request);
                if (transactionsResponse is not OkObjectResult transactionsOk)
                {
                    return transactionsResponse;
                }

                var fullStatement = new
                {
                    AccountInfo = accountInfoOk.Value,
                    AccountSummary = summaryOk.Value,
                    Transactions = transactionsOk.Value
                };

                return Ok(fullStatement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error generating full statement", Error = ex.Message });
            }
        }

        // POST: api/statement/export-pdf
        [HttpPost("export-pdf")]
        public async Task<IActionResult> ExportStatementPdf([FromBody] StatementRequestDto request)
        {
            try
            {
                var statementResponse = await GetFullStatement(request);
                if (statementResponse is not OkObjectResult statementOk)
                {
                    return statementResponse;
                }

                // Simple text-based export (you can enhance with proper PDF library)
                var statement = statementOk.Value;
                var content = $"STATEMENT OF ACCOUNT\n" +
                             $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                             $"Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}\n\n" +
                             $"Account Information:\n" +
                             $"User ID: {request.UserId}\n" +
                             $"Loan ID: {request.LoanId}\n\n" +
                             $"This is a simplified text export. Implement proper PDF generation as needed.\n";

                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/plain", $"statement_{request.UserId}_{DateTime.Now:yyyyMMdd}.txt");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error exporting statement", Error = ex.Message });
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
