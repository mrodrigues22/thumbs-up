using Microsoft.Extensions.Hosting;
using ApprooveItApi.Interfaces;
using ApprooveItApi.Services;

namespace ApprooveItApi.Services;

public class AiProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ISubmissionAnalysisQueue _queue;
    private readonly ILogger<AiProcessingWorker> _logger;

    public AiProcessingWorker(IServiceProvider services, ISubmissionAnalysisQueue queue, ILogger<AiProcessingWorker> logger)
    {
        _services = services;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Processing Worker started");
        await foreach (var submissionId in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var analysis = scope.ServiceProvider.GetRequiredService<IImageAnalysisService>();
                await analysis.AnalyzeSubmissionAsync(submissionId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing submission {SubmissionId}", submissionId);
            }
        }
    }
}
