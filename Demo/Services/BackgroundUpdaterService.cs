using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
namespace Demo.Services;
using Microsoft.Extensions.DependencyInjection;

public class BackgroundUpdaterService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BackgroundUpdaterService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                //register all service in the service file then called and run in the background
                var registerService = scope.ServiceProvider.GetRequiredService<RegisterService>();

                // Call the scoped service
                registerService.UpdateVoucherStatus();
                registerService.CheckPendingBookingsDurationAndUpdate();
                registerService.RealTimeUpdateMemberRank();
                registerService.UpdateRealTimeSchedulBookingsStatus();
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
