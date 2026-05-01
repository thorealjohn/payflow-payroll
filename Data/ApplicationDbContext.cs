using itpayroll.Areas.Identity.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<Earning> Earnings { get; set; }
        public DbSet<Deduction> Deductions { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // AuditLog index (for performance)
            builder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            base.OnModelCreating(builder);
            // Soft delete + active filter
            builder.Entity<ApplicationUser>()
                .HasQueryFilter(u => !u.IsDeleted);

            // Unique Employee Number
            builder.Entity<Employee>()
                .HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            // One User = One Employee
            builder.Entity<Employee>()
                .HasIndex(e => e.UserId)
                .IsUnique();

            // Unique attendance per employee per day
            builder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

            // Deduction relation safety
            builder.Entity<Deduction>()
                .HasOne(d => d.Payroll)
                .WithMany()
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);


            // Prevent duplicate payroll per employee per period
            builder.Entity<Payroll>()
                .HasIndex(p => new { p.EmployeeId, p.PeriodStart, p.PeriodEnd })
                .IsUnique();

            // Payroll → Employee relation
            builder.Entity<Payroll>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Earning relation
            builder.Entity<Earning>()
                .HasOne(e => e.Payroll)
                .WithMany()
                .HasForeignKey(e => e.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Employee>()
            .Property(e => e.BasicSalary)
            .HasPrecision(18, 2);

            builder.Entity<Payroll>()
                .Property(p => p.GrossPay)
                .HasPrecision(18, 2);

            builder.Entity<Payroll>()
                .Property(p => p.NetPay)
                .HasPrecision(18, 2);

            builder.Entity<Payroll>()
                .Property(p => p.TotalDeductions)
                .HasPrecision(18, 2);

            builder.Entity<Earning>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Deduction>()
                .Property(d => d.Amount)
                .HasPrecision(18, 2);
        }

    }
}
