// LoanTrackerJLC/Data/LoanTrackerJLCDbContext.cs
using LoanTrackerJLC.Models;
using Microsoft.EntityFrameworkCore;

namespace LoanTrackerJLC.Data
{
    public class LoanTrackerJLCDbContext : DbContext
    {
        public LoanTrackerJLCDbContext(DbContextOptions<LoanTrackerJLCDbContext> options) : base(options)
        {
        }

        public DbSet<tblUser> tblUsers { get; set; }
        public DbSet<tblImage> tblImages { get; set; }
        public DbSet<tblLoan> tblLoans { get; set; }
        public DbSet<tblLoanTransaction> tblLoanTransactions { get; set; }
        public DbSet<tblPaymentHistory> tblPaymentHistories { get; set; }
        public DbSet<tblAccountAssignment> tblAccountAssignments { get; set; }
        public DbSet<tblTransactionHistory> tblTransactionHistories { get; set; }
        public DbSet<tblPaymentProof> tblPaymentProofs { get; set; }
        public DbSet<tblPerson> tblPersons { get; set; }
        public DbSet<tblLoanType> tblLoanTypes { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure tblUser
            modelBuilder.Entity<tblUser>().ToTable("tblUser");

            // Configure tblLoan
            modelBuilder.Entity<tblLoan>().ToTable("tblLoan");

            // Configure tblLoanTransaction
            modelBuilder.Entity<tblLoanTransaction>().ToTable("tblLoanTransactions");
            modelBuilder.Entity<tblLoanTransaction>()
                .Property(lt => lt.PrincipalDue)
                .HasColumnType("money");
            modelBuilder.Entity<tblLoanTransaction>()
                .Property(lt => lt.InterestDue)
                .HasColumnType("money");
            modelBuilder.Entity<tblLoanTransaction>()
                .Property(lt => lt.PenaltiesDue)
                .HasColumnType("money");
            modelBuilder.Entity<tblLoanTransaction>()
                .Property(lt => lt.TotalDue)
                .HasColumnType("money");
            modelBuilder.Entity<tblLoanTransaction>()
                .Property(lt => lt.WeeklyDue)
                .HasColumnType("money");
            modelBuilder.Entity<tblLoanTransaction>()
                .Property(lt => lt.ServiceFeesDue)
                .HasColumnType("money");

            // Configure tblPaymentHistory
            modelBuilder.Entity<tblPaymentHistory>().ToTable("tblPaymentHistory");
            modelBuilder.Entity<tblPaymentHistory>()
                .Property(ph => ph.Amount)
                .HasColumnType("money");

            // Configure tblAccountAssignment
            modelBuilder.Entity<tblAccountAssignment>().ToTable("tblAccountAssignments");
            modelBuilder.Entity<tblAccountAssignment>()
                .HasOne(a => a.Loan)
                .WithMany()
                .HasForeignKey(a => a.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<tblAccountAssignment>()
                .HasOne(a => a.CollectorUser)
                .WithMany()
                .HasForeignKey(a => a.CollectorUserID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<tblAccountAssignment>()
                .HasOne(a => a.AssignedByUser)
                .WithMany()
                .HasForeignKey(a => a.AssignedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure tblTransactionHistory
            modelBuilder.Entity<tblTransactionHistory>().ToTable("tblTransactionHistory");
            modelBuilder.Entity<tblTransactionHistory>()
                .Property(th => th.Amount)
                .HasColumnType("money");
            modelBuilder.Entity<tblTransactionHistory>()
                .Property(th => th.NewAmountOnFee)
                .HasColumnType("money");

            // Configure tblPaymentProof
            modelBuilder.Entity<tblPaymentProof>().ToTable("tblPaymentProofs");
            modelBuilder.Entity<tblPaymentProof>()
                .HasOne(pp => pp.PaymentHistory)
                .WithMany()
                .HasForeignKey(pp => pp.PaymentHistoryID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure tblPerson
            modelBuilder.Entity<tblPerson>().ToTable("tblPerson");
            modelBuilder.Entity<tblPerson>()
                .HasOne(p => p.User)
                .WithOne(u => u.Person)
                .HasForeignKey<tblPerson>(p => p.UID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}