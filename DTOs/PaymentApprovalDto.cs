using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace LoanTrackerJLC.DTOs
{
    public class PaymentApprovalDto
    {
        [Required]
        public bool IsApproved { get; set; }

        [Required]
        [MaxLength(100)]
        public string ApprovedBy { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        public string? ApprovalNotes { get; set; }
    }
}