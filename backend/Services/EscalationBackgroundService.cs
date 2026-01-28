using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RechnungsfreigabeAPI.Services;

/// <summary>
/// Background service that periodically checks and processes escalation emails
/// </summary>
public class EscalationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EscalationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public EscalationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EscalationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Escalation Background Service started - checking every {Seconds} seconds", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEscalationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Escalation Background Service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Escalation Background Service stopped");
    }

    private async Task ProcessEscalationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<IEscalationEmailService>();
        
        await escalationService.ProcessEscalationEmailsAsync();
    }
}
