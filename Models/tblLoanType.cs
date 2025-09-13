using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanTrackerJLC.Models
{
    [Table("tblLoanType")]
    public class tblLoanType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("LoanTypeID")]
        public int LoanTypeID { get; set; }

        [Column("LoanDescription")]
        [StringLength(150)]
        public string? LoanDescription { get; set; }

        [Column("InterestRate")]
        public double? InterestRate { get; set; }

        [Column("PenaltiesRate")]
        public double? PenaltiesRate { get; set; }

        [Column("MaxTerm")]
        public int? MaxTerm { get; set; }
    }
}