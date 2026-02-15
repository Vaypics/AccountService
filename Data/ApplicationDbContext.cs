using Microsoft.EntityFrameworkCore;
using AccountService.Models;

namespace AccountService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OwnerId)
                    .IsRequired();

                entity.Property(e => e.Type)
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .IsRequired()
                    .HasMaxLength(3); 

                entity.Property(e => e.Balance)
                    .HasPrecision(18, 2);  

                entity.Property(e => e.InterestRate)
                    .HasPrecision(5, 2);   

                entity.Property(e => e.OpenedDate)
                    .IsRequired();

                entity.HasMany(e => e.Transactions)
                    .WithOne()
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);  
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AccountId)
                    .IsRequired();

                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.Type)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasMaxLength(500); 

                entity.Property(e => e.TransactionDate)
                    .IsRequired();

                entity.HasIndex(e => e.AccountId)
                    .HasDatabaseName("IX_Transactions_AccountId");

                entity.HasIndex(e => e.TransactionDate)
                    .HasDatabaseName("IX_Transactions_Date");
            });
        }
    }
}