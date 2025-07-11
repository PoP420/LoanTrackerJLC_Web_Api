using LoanTrackerJLC.Data;
using LoanTrackerJLC.DTOs;
using LoanTrackerJLC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LoanTrackerJLC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectorController : ControllerBase
    {
        private readonly LoanTrackerJLCDbContext _context;

        public CollectorController(LoanTrackerJLCDbContext context)
        {
            _context = context;
        }

        // GET: api/Collector/{collectorId}/assignments
        [HttpGet("{collectorId}/assignments")]
        public async Task<IActionResult> GetAssignedLoans(int collectorId)
        {
            var assignedLoans = await _context.tblAccountAssignments
                .Where(a => a.CollectorUserID == collectorId)
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

        [HttpGet("person/{userId}")]
        public async Task<IActionResult> GetPerson(int userId)
        {
            var person = await _context.tblPersons.FirstOrDefaultAsync(p => p.UID == userId);
            if (person == null) return NotFound(new { Message = "Person not found." });
            return Ok(new { userId = person.UID, address1 = person.address1?.Trim() });
        }

        [HttpGet("{collectorId}/assignments/{loanId}/transactions")]
        public async Task<IActionResult> GetAssignedLoanTransactions(int collectorId, int loanId)
        {
            // First, verify the loan is actually assigned to the collector
            var isAssigned = await _context.tblAccountAssignments
                .AnyAsync(a => a.CollectorUserID == collectorId && a.LoanId == loanId);

            if (!isAssigned)
            {
                return Forbid("This loan is not assigned to the specified collector.");
            }

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

        [HttpPost("payments")]
        public async Task<IActionResult> CreatePayment([FromForm] PaymentRequestDto paymentRequest, IFormFile? paymentProofImage)
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

                // Find the specific loan transaction to mark as paid
                var loanTransaction = await _context.tblLoanTransactions
                    .FirstOrDefaultAsync(lt => lt.Id == paymentRequest.LoanTransactionID && lt.LoanID == paymentRequest.LoanID);

                if (loanTransaction == null)
                {
                    await dbTransaction.RollbackAsync();
                    return NotFound(new { Message = $"Loan transaction with ID {paymentRequest.LoanTransactionID} for Loan {paymentRequest.LoanID} not found." });
                }

                if (loanTransaction.isPaid == true)
                {
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { Message = $"Loan transaction with ID {paymentRequest.LoanTransactionID} is already marked as paid." });
                }

                // Update Loan Transaction
                loanTransaction.isPaid = true;
                loanTransaction.Remarks = $"Paid on {DateTime.UtcNow:yyyy-MM-dd} by User {paymentRequest.UserID}. Amount: {paymentRequest.Amount:C}"; // Example remark
                _context.tblLoanTransactions.Update(loanTransaction);

                // Create Payment History Record
                var payment = new tblPaymentHistory
                {
                    LoanID = paymentRequest.LoanID,
                    TrxnID = loanTransaction.Id, // Link to the tblLoanTransactions.Id
                    Amount = paymentRequest.Amount,
                    userID = paymentRequest.UserID,
                    PDate = paymentRequest.PaymentDate ?? DateTime.UtcNow // Use provided date or default to now
                };
                _context.tblPaymentHistories.Add(payment);
                await _context.SaveChangesAsync(); // Save payment to get its ID for the image

                // Handle Image Upload
                if (paymentProofImage != null && paymentProofImage.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await paymentProofImage.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    var paymentProofRecord = new tblPaymentProof
                    {
                        PaymentHistoryID = payment.Id, // Link to the tblPaymentHistory.Id
                        ImageData = imageBytes,
                        FileType = paymentProofImage.ContentType,
                        FileName = paymentProofImage.FileName,
                        UploadedTimestamp = DateTime.UtcNow
                    };
                    _context.tblPaymentProofs.Add(paymentProofRecord);
                    await _context.SaveChangesAsync(); // Save payment proof
                }

                await dbTransaction.CommitAsync();

                return Ok(new { Message = "Payment created successfully.", Payment = payment });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                // Log the exception (ex)
                // Consider more specific error messages or logging for production
                return StatusCode(500, $"An internal error occurred while processing the payment: {ex.Message}");
            }
        }
    }
}
