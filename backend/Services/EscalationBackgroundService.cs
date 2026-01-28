using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Services;

/// <summary>
/// Background service that periodically checks and processes escalation emails
/// </summary>
public class EscalationBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<EscalationBackgroundService> logger;
    private readonly TimeSpan interval = TimeSpan.FromSeconds(30);

    public EscalationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EscalationBackgroundService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Escalation Background Service started - checking every {Seconds} seconds", interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEscalationsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred in Escalation Background Service");
            }

            await Task.Delay(interval, stoppingToken);
        }

        logger.LogInformation("Escalation Background Service stopped");
    }

    private async Task ProcessEscalationsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<IEscalationEmailService>();
        
        await escalationService.ProcessEscalationEmailsAsync();
    }
}
