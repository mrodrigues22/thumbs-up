using System.Collections.Generic;

namespace ThumbsUpApi.Services;

public interface ISubmissionAnalysisQueue
{
    void Enqueue(Guid submissionId);
    IAsyncEnumerable<Guid> DequeueAsync(CancellationToken ct);
}
