using LoanTrackerJLC.Data;
using LoanTrackerJLC.DTOs;
using LoanTrackerJLC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System; // Added for DateTime
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System.IO; // Required for MemoryStream
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // TODO: Add [Authorize] attribute once JWT token generation and validation is fully implemented in AuthController
    public class LoansController : ControllerBase
    {
        private readonly LoanTrackerJLCDbContext _context;

        public LoansController(LoanTrackerJLCDbContext context)
        {
            _context = context;
        }

         // Enhanced GET: api/loans/client/{userId} - Returns comprehensive loan data
        [HttpGet("client/{userId}")]
        public async Task<IActionResult> GetClientLoanDetails(int userId)
        {
            try
            {
                // First check if user exists
                var userExists = await _context.tblUsers.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    return NotFound(new { 
                        Message = $"User with ID {userId} not found in the system.",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Comprehensive query based on your reference SQL
                var clientLoansData = await (from L in _context.tblLoans
                    join U in _context.tblUsers on L.UserId equals U.UserId
                    where L.UserId == userId && (L.Status == "Active" || L.Status == "Overdue")
                    select new
                    {
                        // Loan Information
                        LoanId = L.LoanId,
                        UserId = L.UserId,
                        UserName = U.UserName,
                        FullName = U.FullName,
                        PIN = U.PIN,
                        LoanTypeID = L.LoanTypeID,
                        Amount = L.Amount,
                        Interest = L.Interest,
                        Total = L.Total,
                        LoanTerm = L.LoanTerm,
                        Status = L.Status,
                        
                        // Related Transactions
                        LoanTransactions = _context.tblLoanTransactions
                            .Where(lt => lt.LoanID == L.LoanId)
                            .OrderBy(lt => lt.DueDate)
                            .Select(lt => new
                            {
                                Id = lt.Id,
                                PrincipalDue = lt.PrincipalDue,
                                InterestDue = lt.InterestDue,
                                PenaltiesDue = lt.PenaltiesDue,
                                TotalDue = lt.TotalDue,
                                DueDate = lt.DueDate,
                                IsPaid = lt.isPaid,
                                Remarks = lt.Remarks,
                                WeeklyDue = lt.WeeklyDue,
                                PenaltyMonthsApplied = lt.PenaltyMonthsApplied,
                                MonthlyDueDate = lt.MonthlyDueDate,
                                ServiceFeesDue = lt.ServiceFeesDue
                            }).ToList(),
                        
                        // Payment History
                        PaymentHistory = _context.tblPaymentHistories
                            .Where(ph => ph.LoanID == L.LoanId)
                            .OrderByDescending(ph => ph.PDate)
                            .Select(ph => new
                            {
                                Id = ph.Id,
                                Amount = ph.Amount,
                                PDate = ph.PDate,
                                TrxnID = ph.TrxnID,
                                UserID = ph.userID,
                                Status = ph.Status
                            }).ToList(),
                        
                        // Latest Transaction History
                        LatestTransaction = _context.tblTransactionHistories
                            .Where(th => th.LoanID == L.LoanId)
                            .OrderByDescending(th => th.PDate)
                            .Select(th => new
                            {
                                Amount = th.Amount,
                                PDate = th.PDate,
                                Remarks = th.Remarks,
                                NewAmountOnFee = th.NewAmountOnFee
                            }).FirstOrDefault()
                    }).ToListAsync();

                if (!clientLoansData.Any())
                {
                    return NotFound(new { 
                        Message = $"No active loans found for user ID {userId}. User exists but has no active loans.",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Calculate summary information
                var summaryData = clientLoansData.Select(loan => new
                {
                    // Basic loan info
                    LoanId = loan.LoanId,
                    UserId = loan.UserId,
                    ClientFullName = loan.FullName,
                    UserName = loan.UserName,
                    LoanAmount = loan.Amount,
                    Interest = loan.Interest,
                    Total = loan.Total,
                    LoanTerm = loan.LoanTerm,
                    Status = loan.Status,
                    
                    // Calculated fields
                    OutstandingBalance = loan.LoanTransactions
                        .Where(lt => lt.IsPaid == false)
                        .Sum(lt => lt.TotalDue ?? 0),
                    
                    TotalPaid = loan.PaymentHistory.Sum(ph => ph.Amount ?? 0),
                    
                    NextPaymentDue = loan.LoanTransactions
                        .Where(lt => lt.IsPaid == false && lt.DueDate >= DateTime.Today)
                        .OrderBy(lt => lt.DueDate)
                        .FirstOrDefault(),
                    
                    OverduePayments = loan.LoanTransactions
                        .Where(lt => lt.IsPaid == false && lt.DueDate < DateTime.Today)
                        .OrderBy(lt => lt.DueDate)
                        .ToList(),
                    
                    // Detailed data
                    LoanTransactions = loan.LoanTransactions,
                    PaymentHistory = loan.PaymentHistory,
                    LatestTransaction = loan.LatestTransaction
                }).ToList();

                return Ok(new
                {
                    Message = "Client loan data retrieved successfully",
                    UserId = userId,
                    TotalLoans = summaryData.Count,
                    Timestamp = DateTime.UtcNow,
                    Data = summaryData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving client loan data",
                    Error = ex.Message,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }   

        // **ENHANCED ENDPOINT: GET: api/loans/user/{userId}/payment-history - Get complete payment schedule with smart filtering**
        [HttpGet("user/{userId}/payment-history")]
        public async Task<IActionResult> GetUserPaymentHistory(int userId)
        {
            try
            {
                // First check if user exists
                var userExists = await _context.tblUsers.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    return NotFound(new { 
                        Message = $"User with ID {userId} not found in the system.",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Get all payment history and loan transactions for this user
                var allPaymentData = await (from ph in _context.tblPaymentHistories
                    join l in _context.tblLoans on ph.LoanID equals l.LoanId
                    where l.UserId == userId
                    select new
                    {
                        PaymentHistory = ph,
                        LoanId = ph.LoanID,
                        TransactionId = ph.TrxnID
                    }).ToListAsync();

                var loanTransactions = await (from lt in _context.tblLoanTransactions
                    join l in _context.tblLoans on lt.LoanID equals l.LoanId
                    where l.UserId == userId
                    select lt).ToListAsync();

                // **ENHANCED LOGIC: Get pending payments to filter overdue, but keep scheduled payments**
                var pendingPayments = allPaymentData
                    .Where(p => p.PaymentHistory.Status?.ToLower() == "pending")
                    .Select(p => p.TransactionId)
                    .ToHashSet();

                var paymentHistoryResult = new List<object>();

                // Add actual payment history (submitted payments)
                foreach (var paymentData in allPaymentData)
                {
                    var ph = paymentData.PaymentHistory;
                    paymentHistoryResult.Add(new
                    {
                        Id = ph.Id,
                        LoanId = ph.LoanID,
                        Amount = ph.Amount,
                        PaymentDate = ph.PDate ?? ph.SubmittedDate,
                        Status = ph.Status ?? "Unknown",
                        TransactionId = ph.TrxnID,
                        ReceiptUrl = _context.tblPaymentProofs.Any(pp => pp.PaymentHistoryID == ph.Id) 
                            ? $"/api/loans/payments/{ph.Id}/receipt" 
                            : null,
                        ApprovalDate = ph.ApprovedDate,
                        RejectionReason = ph.RejectionReason,
                        Notes = ph.ApprovalNotes,
                        SubmittedDate = ph.SubmittedDate,
                        ApprovedBy = ph.ApprovedBy,
                        Remarks = ph.Remarks,
                        IsActualPayment = true,
                        ReferenceNumber = $"PAY-{ph.Id:D6}",
                        Description = "Payment Submitted",
                        PaymentMethod = "Pending Review"
                    });
                }

                // **ENHANCED: Add ALL unpaid transactions (both scheduled and overdue)**
                foreach (var transaction in loanTransactions)
                {
                    bool isUnpaid = transaction.isPaid == false || transaction.isPaid == null;
                    
                    if (isUnpaid)
                    {
                        bool isOverdue = transaction.DueDate < DateTime.Today;
                        bool hasPendingPayment = pendingPayments.Contains(transaction.Id);
                        
                        // **KEY LOGIC: Only hide overdue if there's a pending payment, but always show scheduled**
                        bool shouldShow = !isOverdue || (isOverdue && !hasPendingPayment);
                        
                        if (shouldShow)
                        {
                            string status = isOverdue ? "Overdue" : "Scheduled";
                            
                            paymentHistoryResult.Add(new
                            {
                                Id = $"{status.ToLower()}_{transaction.Id}",
                                LoanId = transaction.LoanID,
                                Amount = transaction.TotalDue,
                                PaymentDate = transaction.DueDate,
                                Status = status,
                                TransactionId = transaction.Id,
                                ReceiptUrl = (string?)null,
                                ApprovalDate = (DateTime?)null,
                                RejectionReason = (string?)null,
                                Notes = (string?)null,
                                SubmittedDate = (DateTime?)null,
                                ApprovedBy = (string?)null,
                                Remarks = isOverdue 
                                    ? $"Payment overdue since {transaction.DueDate:yyyy-MM-dd}" 
                                    : $"Payment due on {transaction.DueDate:yyyy-MM-dd}",
                                IsActualPayment = false,
                                // Additional fields for scheduled/overdue payments
                                PrincipalDue = transaction.PrincipalDue,
                                InterestDue = transaction.InterestDue,
                                PenaltiesDue = transaction.PenaltiesDue,
                                ServiceFeesDue = transaction.ServiceFeesDue,
                                DueDate = transaction.DueDate,
                                ReferenceNumber = $"SCH-{transaction.Id:D6}",
                                Description = isOverdue ? "Overdue Payment" : "Scheduled Payment",
                                PaymentMethod = "Pending"
                            });
                        }
                    }
                }

                // Sort by payment date descending
                var sortedResults = paymentHistoryResult
                    .OrderByDescending(p => p.GetType().GetProperty("PaymentDate")?.GetValue(p) as DateTime?)
                    .ToList();

                return Ok(sortedResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving payment history",
                    Error = ex.Message,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }


        [HttpGet("user/{userId}/payment-status-history")]
        public async Task<IActionResult> GetUserPaymentStatusHistory(int userId)
        {
            try
            {
                // First check if user exists
                var userExists = await _context.tblUsers.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    return NotFound(new { 
                        Message = $"User with ID {userId} not found in the system.",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Get payment history with only specific statuses
                var paymentHistory = await _context.tblPaymentHistories
                    .Where(ph => ph.LoanID.HasValue && ph.Status != null && 
                                (ph.Status == "Approved" || ph.Status == "Pending" || ph.Status == "Rejected"))
                    .Join(_context.tblLoans,
                        ph => ph.LoanID,
                        l => l.LoanId,
                        (ph, l) => new { PaymentHistory = ph, Loan = l })
                    .Where(j => j.Loan.UserId == userId)
                    .OrderByDescending(j => j.PaymentHistory.PDate ?? j.PaymentHistory.SubmittedDate)
                    .Select(j => new
                    {
                        Id = j.PaymentHistory.Id,
                        LoanId = j.PaymentHistory.LoanID,
                        Amount = j.PaymentHistory.Amount,
                        PaymentDate = j.PaymentHistory.PDate ?? j.PaymentHistory.SubmittedDate,
                        Status = j.PaymentHistory.Status,
                        TransactionId = j.PaymentHistory.TrxnID,
                        ReceiptUrl = _context.tblPaymentProofs.Any(pp => pp.PaymentHistoryID == j.PaymentHistory.Id) 
                            ? $"/api/loans/payments/{j.PaymentHistory.Id}/receipt" 
                            : null,
                        ApprovalDate = j.PaymentHistory.ApprovedDate,
                        RejectionReason = j.PaymentHistory.RejectionReason,
                        Notes = j.PaymentHistory.ApprovalNotes,
                        SubmittedDate = j.PaymentHistory.SubmittedDate,
                        ApprovedBy = j.PaymentHistory.ApprovedBy,
                        Remarks = j.PaymentHistory.Remarks,
                        IsActualPayment = true,
                        ReferenceNumber = $"PAY-{j.PaymentHistory.Id:D6}",
                        Description = "Payment Submitted",
                        PaymentMethod = j.PaymentHistory.Status == "Pending" ? "Pending Review" : "GCash"
                    })
                    .ToListAsync();

                if (!paymentHistory.Any())
                {
                    return NotFound(new { 
                        Message = "No payment history found with statuses 'Approved', 'Pending', or 'Rejected' for this user.",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                return Ok(paymentHistory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving payment status history",
                    Error = ex.Message,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // GET: api/loans/client/{userId}/summary - Lightweight summary endpoint
        [HttpGet("client/{userId}/summary")]
        public async Task<IActionResult> GetClientLoanSummary(int userId)
        {
            try
            {
                var summary = await (from L in _context.tblLoans
                    join U in _context.tblUsers on L.UserId equals U.UserId
                    where L.UserId == userId && L.Status == "Active"
                    select new
                    {
                        LoanId = L.LoanId,
                        ClientFullName = U.FullName,
                        LoanAmount = L.Amount,
                        Total = L.Total,
                        Status = L.Status,
                        OutstandingBalance = _context.tblLoanTransactions
                            .Where(lt => lt.LoanID == L.LoanId && lt.isPaid == false)
                            .Sum(lt => lt.TotalDue ?? 0),
                        NextPaymentAmount = _context.tblLoanTransactions
                            .Where(lt => lt.LoanID == L.LoanId && lt.isPaid == false && lt.DueDate >= DateTime.Today)
                            .OrderBy(lt => lt.DueDate)
                            .Select(lt => lt.TotalDue)
                            .FirstOrDefault(),
                        NextPaymentDate = _context.tblLoanTransactions
                            .Where(lt => lt.LoanID == L.LoanId && lt.isPaid == false && lt.DueDate >= DateTime.Today)
                            .OrderBy(lt => lt.DueDate)
                            .Select(lt => lt.DueDate)
                            .FirstOrDefault()
                    }).ToListAsync();

                if (!summary.Any())
                {
                    return NotFound(new { Message = "No active loans found for this client." });
                }

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving loan summary", Error = ex.Message });
            }
        }

        // GET: api/loans/debug/users - Debug endpoint to see available users
        [HttpGet("debug/users")]
        public async Task<IActionResult> GetAvailableUsers()
        {
            try
            {
                var users = await _context.tblUsers
                    .Select(u => new
                    {
                        UserId = u.UserId,
                        UserName = u.UserName,
                        FullName = u.FullName,
                        HasLoans = _context.tblLoans.Any(l => l.UserId == u.UserId)
                    })
                    .Take(20) // Limit to first 20 users
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Available users (first 20)",
                    Count = users.Count,
                    Users = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving users", Error = ex.Message });
            }
        }

        // Existing endpoints remain the same...
        [HttpGet("{loanId}/transactions")]
        public async Task<IActionResult> GetLoanTransactions(int loanId)
        {
            var transactions = await _context.tblLoanTransactions
                                           .Where(t => t.LoanID == loanId)
                                           .OrderByDescending(t => t.DueDate)
                                           .ToListAsync();

            if (!transactions.Any())
            {
                return NotFound(new { Message = "No transactions found for this loan." });
            }

            return Ok(transactions);
        }

        // GET: api/loans/{loanId}/payments
        [HttpGet("{loanId}/payments")]
        public async Task<IActionResult> GetClientPaymentHistory(int loanId)
        {
            var payments = await _context.tblPaymentHistories
                                         .Where(p => p.LoanID == loanId)
                                         .OrderByDescending(p => p.PDate) // Or by Id
                                         .ToListAsync();

            if (!payments.Any())
            {
                return NotFound(new { Message = "No payment history found for this loan." });
            }

            return Ok(payments);
        }

        // GET: api/loans/collector/{collectorUserId}/assigned
        [HttpGet("collector/{collectorUserId}/assigned")]
        public async Task<IActionResult> GetAssignedLoansForCollector(int collectorUserId)
        {
            var assignedLoans = await _context.tblAccountAssignments
                .Where(a => a.CollectorUserID == collectorUserId && a.Status == "Active")
                .Include(a => a.Loan)
                .ThenInclude(l => l.User)
                .Select(a => new AssignedLoanDto
                {
                    Loan = a.Loan,
                    Client = a.Loan.User
                })
                .ToListAsync();

            if (!assignedLoans.Any())
            {
                return NotFound(new { Message = "No loans assigned to this collector." });
            }

            return Ok(assignedLoans);
        }

           // POST: api/loans/payments - Submit payment for approval (no immediate loan transaction update)
        [HttpPost("payments")]
        public async Task<IActionResult> SubmitPayment([FromForm] PaymentRequestDto paymentRequest, IFormFile? paymentProofImage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate LoanID
                var loan = await _context.tblLoans.FirstOrDefaultAsync(l => l.LoanId == paymentRequest.LoanID);
                if (loan == null)
                {
                    await dbTransaction.RollbackAsync();
                    return NotFound(new { Message = $"Loan with ID {paymentRequest.LoanID} not found." });
                }

                // Validate UserID
                var userExists = await _context.tblUsers.AnyAsync(u => u.UserId == paymentRequest.UserID);
                if (!userExists)
                {
                    await dbTransaction.RollbackAsync();
                    return NotFound(new { Message = $"User with ID {paymentRequest.UserID} not found." });
                }

                // Validate the loan transaction exists
                var loanTransaction = await _context.tblLoanTransactions
                    .FirstOrDefaultAsync(lt => lt.Id == paymentRequest.LoanTransactionID && lt.LoanID == paymentRequest.LoanID);

                if (loanTransaction == null)
                {
                    await dbTransaction.RollbackAsync();
                    return NotFound(new { Message = $"Loan transaction with ID {paymentRequest.LoanTransactionID} for Loan {paymentRequest.LoanID} not found." });
                }

                // Check if transaction is already fully paid
                if (loanTransaction.isPaid == true)
                {
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { Message = $"Loan transaction with ID {paymentRequest.LoanTransactionID} is already marked as paid." });
                }

                // Validate payment amount doesn't exceed total due
                decimal totalDue = Math.Round(loanTransaction.TotalDue ?? 0, 2);
                decimal amount = Math.Round(paymentRequest.Amount, 2);
                if (amount > totalDue)
                {
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { Message = $"Payment amount {amount:C} exceeds total due {totalDue:C}." });
                }


                // Create Payment History Record with PENDING status
                var payment = new tblPaymentHistory
                {
                    LoanID = paymentRequest.LoanID,
                    TrxnID = loanTransaction.Id,
                    Amount = paymentRequest.Amount,
                    userID = paymentRequest.UserID,
                    PDate = paymentRequest.PaymentDate ?? DateTime.UtcNow,
                    Status = "Pending", // New field - payment awaiting approval
                    SubmittedDate = DateTime.UtcNow, // New field - when payment was submitted
                    Remarks = $"Payment submitted for Transaction ID {loanTransaction.Id}. Amount: {paymentRequest.Amount:C}. Awaiting approval."
                };

                _context.tblPaymentHistories.Add(payment);
                await _context.SaveChangesAsync(); // Save payment to get its ID for the image

                // Handle Image Upload (Receipt)
                if (paymentProofImage != null && paymentProofImage.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await paymentProofImage.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    var paymentProofRecord = new tblPaymentProof
                    {
                        PaymentHistoryID = payment.Id,
                        ImageData = imageBytes,
                        FileType = paymentProofImage.ContentType,
                        FileName = paymentProofImage.FileName,
                        UploadedTimestamp = DateTime.UtcNow
                    };
                    _context.tblPaymentProofs.Add(paymentProofRecord);
                    await _context.SaveChangesAsync();
                }

                await dbTransaction.CommitAsync();

                return Ok(new
                {
                    Message = "Payment submitted successfully and is pending approval.",
                    PaymentId = payment.Id,
                    Status = "Pending",
                    SubmittedAmount = paymentRequest.Amount,
                    TransactionId = loanTransaction.Id,
                    EstimatedApprovalTime = "24-48 hours",
                    HasReceipt = paymentProofImage != null
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, $"An internal error occurred while submitting the payment: {ex.Message}");
            }
        }

        // PUT: api/loans/payments/{paymentId}/approve - Admin/Clerk approves or rejects payment
        [HttpPut("payments/{paymentId}/approve")]
        [Authorize(Roles = "Admin,Clerk")] // Requires admin or clerk role
        public async Task<IActionResult> ApprovePayment(int paymentId, [FromBody] PaymentApprovalDto approvalDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Find the payment record
                var payment = await _context.tblPaymentHistories
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    await dbTransaction.RollbackAsync();
                    return NotFound(new { Message = $"Payment with ID {paymentId} not found." });
                }

                // Check if payment is still pending
                if (payment.Status != "Pending")
                {
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { Message = $"Payment is already {payment.Status}. Cannot modify." });
                }

                // Update payment status
                payment.Status = approvalDto.IsApproved ? "Approved" : "Rejected";
                payment.ApprovedBy = approvalDto.ApprovedBy;
                payment.ApprovedDate = DateTime.UtcNow;
                payment.ApprovalNotes = approvalDto.ApprovalNotes;
                payment.RejectionReason = approvalDto.IsApproved ? null : approvalDto.RejectionReason;

                if (approvalDto.IsApproved)
                {
                    // ONLY UPDATE LOAN TRANSACTION IF APPROVED
                    var loanTransaction = await _context.tblLoanTransactions
                        .FirstOrDefaultAsync(lt => lt.Id == payment.TrxnID);

                    if (loanTransaction != null)
                    {
                        decimal originalTotalDue = loanTransaction.TotalDue ?? 0;
                        decimal paymentAmount = payment.Amount ?? 0;
                        decimal newTotalDue = Math.Max(0, originalTotalDue - paymentAmount);

                        // Update loan transaction
                        loanTransaction.TotalDue = newTotalDue;
                        loanTransaction.isPaid = newTotalDue <= 0;
                        loanTransaction.Remarks = $"Payment approved on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} by {approvalDto.ApprovedBy}. " +
                                                 $"Payment Amount: {paymentAmount:C}. Previous Balance: {originalTotalDue:C}. " +
                                                 $"New Balance: {newTotalDue:C}";

                        _context.tblLoanTransactions.Update(loanTransaction);

                        // Update overall loan status if all transactions are paid
                        await UpdateLoanStatusIfFullyPaid(payment.LoanID ?? 0);
                    }

                    payment.Remarks = $"Payment approved by {approvalDto.ApprovedBy} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. " +
                                     $"Applied to Transaction ID {payment.TrxnID}.";
                }
                else
                {
                    payment.Remarks = $"Payment rejected by {approvalDto.ApprovedBy} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. " +
                                     $"Reason: {approvalDto.RejectionReason}";
                }

                _context.tblPaymentHistories.Update(payment);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(new
                {
                    Message = $"Payment {(approvalDto.IsApproved ? "approved" : "rejected")} successfully.",
                    PaymentId = payment.Id,
                    Status = payment.Status,
                    ProcessedBy = approvalDto.ApprovedBy,
                    ProcessedDate = payment.ApprovedDate,
                    Notes = approvalDto.IsApproved ? approvalDto.ApprovalNotes : approvalDto.RejectionReason
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, $"An internal error occurred while processing the approval: {ex.Message}");
            }
        }

        // GET: api/loans/payments/pending - Get all pending payments for admin review
        [HttpGet("payments/pending")]
        [Authorize(Roles = "Admin,Clerk")]
        public async Task<IActionResult> GetPendingPayments()
        {
           try
            {
                var pendingPayments = await _context.tblPaymentHistories
                    .Where(p => p.Status == "Pending")
                    .OrderBy(p => p.SubmittedDate)
                    .Select(p => new
                    {
                        Id = p.Id,
                        LoanID = p.LoanID,
                        TrxnID = p.TrxnID,
                        Amount = p.Amount,
                        SubmittedDate = p.SubmittedDate,
                        userID = p.userID,
                        Remarks = p.Remarks,
                        // Fixed: Use null-conditional operator and handle nullable TimeSpan
                        WaitingDays = p.SubmittedDate.HasValue 
                            ? (DateTime.UtcNow - p.SubmittedDate.Value).Days 
                            : 0,
                        HasReceipt = _context.tblPaymentProofs.Any(pp => pp.PaymentHistoryID == p.Id)
                    })
                    .ToListAsync();

                return Ok(new { 
                    Message = $"Found {pendingPayments.Count} pending payments.", 
                    PendingPayments = pendingPayments 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An internal error occurred while retrieving pending payments: {ex.Message}");
            }
        }

        // GET: api/loans/payments/{paymentId}/receipt - Get payment receipt image
        [HttpGet("payments/{paymentId}/receipt")]
        [Authorize(Roles = "Admin,Clerk")]
        public async Task<IActionResult> GetPaymentReceipt(int paymentId)
        {
            try
            {
                var paymentProof = await _context.tblPaymentProofs
                    .FirstOrDefaultAsync(pp => pp.PaymentHistoryID == paymentId);

                if (paymentProof == null)
                {
                    return NotFound(new { Message = "Receipt not found for this payment." });
                }

                return File(paymentProof.ImageData, paymentProof.FileType ?? "image/jpeg", paymentProof.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the receipt: {ex.Message}");
            }
        }

        // Helper method to update loan status when all transactions are paid
        private async Task UpdateLoanStatusIfFullyPaid(int loanId)
        {
            try
            {
                var hasUnpaidTransactions = await _context.tblLoanTransactions
                    .AnyAsync(lt => lt.LoanID == loanId && (lt.isPaid == false || lt.isPaid == null));

                if (!hasUnpaidTransactions)
                {
                    var loan = await _context.tblLoans.FirstOrDefaultAsync(l => l.LoanId == loanId);
                    if (loan != null)
                    {
                        loan.Status = "Fully Paid";
                        _context.tblLoans.Update(loan);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating loan status: {ex.Message}");
            }
        }
    }
}
