using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SwimmingTrackSystem.Models;

namespace SwimmingTrackSystem.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = ((App)Application.Current)._configuration;
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.37-mysql"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("products");

            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.Time).HasMaxLength(255);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("settings");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EnterIp).HasMaxLength(255);
            entity.Property(e => e.ExitIp)
                .HasMaxLength(255)
                .HasColumnName("ExitIP");
            entity.Property(e => e.Login).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PosTerminalIp).HasMaxLength(255);
            entity.Property(e => e.RegNumber).HasMaxLength(255);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("transactions");

            entity.Property(e => e.Amount).HasPrecision(10);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.ErrorMessage).HasMaxLength(255);
            entity.Property(e => e.ExpireDate).HasColumnType("datetime");
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.TrackDate).HasColumnType("datetime");
            entity.Property(e => e.TypeTransaction).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
