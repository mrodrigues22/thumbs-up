using System.Threading.Channels;

namespace ThumbsUpApi.Services;

public class SubmissionAnalysisQueue : ISubmissionAnalysisQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid submissionId)
    {
        _channel.Writer.TryWrite(submissionId);
    }

    public async IAsyncEnumerable<Guid> DequeueAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (await _channel.Reader.WaitToReadAsync(ct))
        {
            while (_channel.Reader.TryRead(out var id))
            {
                yield return id;
            }
        }
    }
}
