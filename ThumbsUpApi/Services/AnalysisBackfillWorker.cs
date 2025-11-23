using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Services;

/// <summary>
/// On startup, requeues submissions whose content features are missing or stuck so the AI pipeline can backfill them.
/// </summary>
public class AnalysisBackfillWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ISubmissionAnalysisQueue _queue;
    private readonly ILogger<AnalysisBackfillWorker> _logger;

    public AnalysisBackfillWorker(IServiceProvider services, ISubmissionAnalysisQueue queue, ILogger<AnalysisBackfillWorker> logger)
    {
        _services = services;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analysis backfill worker started");

        // Give EF migrations a moment to run before querying.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await QueueStaleSubmissionsAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue stale submissions for analysis");
            }

            // Run once at startup, then exit loop to avoid repeated replays.
            break;
        }
    }

    private async Task QueueStaleSubmissionsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var staleCutoff = DateTime.UtcNow.AddMinutes(-5);

        var staleSubmissionIds = await db.Submissions
            .AsNoTracking()
            .Where(s => s.MediaFiles.Any(m => m.FileType == MediaFileType.Image))
            .Where(s => s.ContentFeature == null
                || s.ContentFeature.AnalysisStatus == ContentFeatureStatus.Failed
                || (s.ContentFeature.AnalysisStatus == ContentFeatureStatus.Pending && (s.ContentFeature.LastAnalyzedAt == null || s.ContentFeature.LastAnalyzedAt < staleCutoff)))
            .Select(s => s.Id)
            .Distinct()
            .ToListAsync(ct);

        foreach (var submissionId in staleSubmissionIds)
        {
            _queue.Enqueue(submissionId);
        }

        if (staleSubmissionIds.Count > 0)
        {
            _logger.LogInformation("Enqueued {Count} submissions for re-analysis", staleSubmissionIds.Count);
        }
        else
        {
            _logger.LogInformation("No submissions required backfill at startup");
        }
    }
}
