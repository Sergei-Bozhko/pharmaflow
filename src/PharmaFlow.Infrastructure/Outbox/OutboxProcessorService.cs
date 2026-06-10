using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PharmaFlow.Infrastructure.Outbox;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    OutboxOptions options,
    ILogger<OutboxProcessorService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Fresh scope per tick: AppDbContext + dispatcher are scoped; this host is a singleton.
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                await processor.ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.BatchFailed(logger, ex);
            }
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception?> BatchFailedMessage =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(1, nameof(BatchFailed)),
                "Outbox batch failed; retrying next tick.");

        public static void BatchFailed(ILogger logger, Exception ex) =>
            BatchFailedMessage(logger, ex);
    }
}