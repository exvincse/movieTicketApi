using Microsoft.EntityFrameworkCore;
using movieTickApi.Models;

namespace movieTickApi.Service
{
        public class TokenCleanupService : BackgroundService
        {
                private readonly IServiceProvider _serviceProvider;

                public TokenCleanupService(IServiceProvider serviceProvider)
                {
                        _serviceProvider = serviceProvider;
                }

                protected override async Task ExecuteAsync(CancellationToken stoppingToken)
                {
                        await CleanupExpiredTokens();
                        while (!stoppingToken.IsCancellationRequested)
                        {
                                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                                await CleanupExpiredTokens();
                        }
                }

                private async Task CleanupExpiredTokens()
                {
                        var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<WebDbContext>();

                        var expiredTokens = await context.Token
                            .Where(t => t.ExpiresAt < DateTime.UtcNow && !t.IsRevoked).ToListAsync();

                        foreach (var token in expiredTokens)
                        {
                                token.IsRevoked = true;
                        }

                        await context.SaveChangesAsync();
                }
        }

}
