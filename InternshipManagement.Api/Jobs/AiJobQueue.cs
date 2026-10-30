using InternshipManagement.Application.Interfaces;
using System.Threading.Channels;

namespace InternshipManagement.Api.Jobs
{
    /// <summary>
    /// In-memory, bounded queue between the HTTP requests (writers) and the AiJobWorker (readers).
    /// Bounded on purpose: when n8n/Gemini is slow the API says "busy" instead of piling up work without limit.
    /// In-memory means one API instance; see the notes for what scaling out would need.
    /// </summary>
    public sealed class AiJobQueue : IAiJobQueue
    {
        public const int Capacity = 50;

        private readonly Channel<AiJobWork> _channel = Channel.CreateBounded<AiJobWork>(
            new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,   // TryWrite returns false when full
                SingleReader = false,
                SingleWriter = false
            });

        public bool TryEnqueue(AiJobWork work) => _channel.Writer.TryWrite(work);

        public ChannelReader<AiJobWork> Reader => _channel.Reader;
    }
}
