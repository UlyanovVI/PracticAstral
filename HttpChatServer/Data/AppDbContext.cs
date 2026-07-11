using Microsoft.EntityFrameworkCore;
using HttpChatShared.Models;

namespace HttpChatServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ChatMessage> Messages { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatMessage>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.From).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(1000);

            entity.Property(e => e.Timestamp)
                .HasConversion(
                    v => v.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    v => DateTime.Parse(v)
                );
        });

        modelBuilder.Entity<User>(entity => {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_Users_Name");
        });
    }
}
