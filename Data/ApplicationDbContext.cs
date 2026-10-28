using EverestRemit.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace EverestRemit.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<KycApplication> KycApplications => Set<KycApplication>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<RemittanceTransfer> Transfers => Set<RemittanceTransfer>();
    public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<KycApplication>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<ExchangeRate>().Property(x => x.Rate).HasPrecision(18, 6);
        b.Entity<Deposit>().Property(x => x.Amount).HasPrecision(18, 2);
        b.Entity<RemittanceTransfer>().Property(x => x.SendAmount).HasPrecision(18, 2);
        b.Entity<RemittanceTransfer>().Property(x => x.ExchangeRate).HasPrecision(18, 6);
        b.Entity<RemittanceTransfer>().Property(x => x.ReceiveAmountNpr).HasPrecision(18, 2);
        b.Entity<Beneficiary>().HasOne(x => x.User).WithMany(x => x.Beneficiaries).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Deposit>().HasOne(x => x.User).WithMany(x => x.Deposits).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RemittanceTransfer>().HasOne(x => x.User).WithMany(x => x.Transfers).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RemittanceTransfer>().HasOne(x => x.Beneficiary).WithMany().HasForeignKey(x => x.BeneficiaryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RemittanceTransfer>().HasOne(x => x.Deposit).WithMany().HasForeignKey(x => x.DepositId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .IsRequired();

            entity.Property(x => x.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
