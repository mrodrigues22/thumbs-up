using System.Collections.Generic;

namespace ApprooveItApi.Services;

public interface ISubmissionAnalysisQueue
{
    void Enqueue(Guid submissionId);
    IAsyncEnumerable<Guid> DequeueAsync(CancellationToken ct);
}
