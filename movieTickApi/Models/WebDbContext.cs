using Microsoft.EntityFrameworkCore;
using movieTickApi.Models.Ticket;
using movieTickApi.Models.Users;

namespace movieTickApi.Models
{
        public class WebDbContext : DbContext
        {
                public WebDbContext(DbContextOptions<WebDbContext> options) : base(options)
                {
                }

                public DbSet<Token> Token { get; set; }
                public DbSet<UserRefreshTokens> UserRefreshTokens { get; set; }
                public DbSet<User> User { get; set; }
                public DbSet<UserProfile> UserProfile { get; set; }
                public DbSet<OtpVerification> OtpVerification { get; set; }
                public DbSet<Locations> Locations { get; set; }
                public DbSet<TicketCategory> TicketCategory { get; set; }
                public DbSet<TicketLanguage> TicketLanguage { get; set; }
                public DbSet<TicketDetailMain> TicketDetailMain { get; set; }
                public DbSet<TicketDetail> TicketDetail { get; set; }
                public DbSet<TicketPaymentStatus> TicketPaymentStatus { get; set; }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                        modelBuilder.Entity<UserProfile>()
                            .HasOne(t => t.User)
                            .WithMany(u => u.UserProfile)
                            .HasForeignKey(t => t.UserId);

                        modelBuilder.Entity<UserProfile>()
                                .Property(u => u.UserNo)
                                .UseIdentityColumn();

                        modelBuilder.Entity<User>()
                                .Property(u => u.UserNo)
                                .UseIdentityColumn();

                        modelBuilder.Entity<Token>()
                            .HasOne(t => t.UserRefreshTokens)
                            .WithMany(u => u.Token)
                            .HasForeignKey(t => t.UserId);

                        modelBuilder.Entity<TicketDetail>()
                            .HasOne(t => t.TicketDetailMain)
                            .WithMany(u => u.TicketDetail)
                            .HasForeignKey(t => t.TicketDetailMainId);

                        modelBuilder.Entity<TicketPaymentStatus>()
                                .HasKey(p => p.StatusId);

                        modelBuilder.Entity<TicketDetailMain>()
                            .HasOne(t => t.TicketPaymentStatus)
                            .WithMany(p => p.TicketDetailMain)
                            .HasForeignKey(t => t.TicketStatusId);
                }
        }
}
