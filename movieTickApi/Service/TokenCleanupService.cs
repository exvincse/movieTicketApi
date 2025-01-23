using movieTickApi.Models;
using Microsoft.EntityFrameworkCore;

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
                                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // 每 30 分鐘執行一次
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
