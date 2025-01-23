using Microsoft.EntityFrameworkCore;

namespace movieTickApi.Models
{
        public class WebDbContext : DbContext
        {
                public WebDbContext(DbContextOptions<WebDbContext> options): base(options)
                {
                }

                public DbSet<Token> Token { get; set; }

                public DbSet<UserRefreshTokens> UserRefreshTokens { get; set; }

                public DbSet<User> User { get; set; }

                public DbSet<UserProfile> UserProfile { get; set; }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                        modelBuilder.Entity<UserProfile>()
                            .HasOne(t => t.User)
                            .WithMany(u => u.UserProfile)
                            .HasForeignKey(t => t.UserId);

                        modelBuilder.Entity<Token>()
                            .HasOne(t => t.UserRefreshTokens)
                            .WithMany(u => u.Token)
                            .HasForeignKey(t => t.UserId);
                }
        }
}
