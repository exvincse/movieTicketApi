using Microsoft.EntityFrameworkCore;
using movieTickApi.Models;

namespace movieTickApi.Service
{
        public class PayPalStatusService : BackgroundService
        {
                private readonly IServiceProvider _serviceProvider;

                public PayPalStatusService(IServiceProvider serviceProvider)
                {
                        _serviceProvider = serviceProvider;
                }

                protected override async Task ExecuteAsync(CancellationToken stoppingToken)
                {
                        await TicketDetailMainUpDate();
                        while (!stoppingToken.IsCancellationRequested)
                        {
                                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                                await TicketDetailMainUpDate();
                        }
                }

                private async Task TicketDetailMainUpDate()
                {
                        var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<WebDbContext>();

                        var ticket = await context.TicketDetailMain
                            .Where(x => x.CreateDateTime.AddMinutes(5) < DateTime.UtcNow && x.TicketStatusId == 2)
                            .ExecuteUpdateAsync(y => y.SetProperty(z => z.TicketStatusId, 3));

                        await context.SaveChangesAsync();
                }
        }

}
