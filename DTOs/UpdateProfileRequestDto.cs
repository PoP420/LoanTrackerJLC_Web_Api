namespace LoanTrackerJLC.DTOs
{
    public class UpdateProfileRequest
    {
      public int UserId { get; set; }
        public int UserType { get; set; }
        public string? FirstName { get; set; } // Nullable to match tblPerson
        public string? LastName { get; set; } // Nullable to match tblPerson
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; } // Optional for collectors
    }
}
