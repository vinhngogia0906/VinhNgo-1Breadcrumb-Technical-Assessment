using LibraryApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Infrastructure.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(254);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Book>(b =>
        {
            b.ToTable("books");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
            b.Ignore(x => x.IsAvailable);

            b.HasOne(x => x.Owner)
                .WithMany(u => u.OwnedBooks)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Borrower)
                .WithMany(u => u.BorrowedBooks)
                .HasForeignKey(x => x.BorrowerId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.Title);
            b.HasIndex(x => x.BorrowerId);
        });
    }
}
