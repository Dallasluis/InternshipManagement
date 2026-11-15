using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Application.Services
{
    /// <summary>
    /// The read-only assistant for companies. The app sends the new message plus the earlier turns; nothing is stored.
    /// The AI can only read (through three read-only tools) and talk: it cannot change anything.
    /// </summary>
    public class CompanyChatService : ICompanyChatService
    {
        // Must match the Webhook node path in the n8n "company chat" workflow.
        private const string WorkflowPath = "company/chat";
        private const string RunTokenPurpose = "company-chat";

        private const int MaxMessageChars = 1000;
        private const int MaxHistoryTurns = 10;
        private const int MaxTurnChars = 1500;
        private const int MaxHistoryChars = 6000;
        private const int MaxReplyChars = 4000;

        private const int MaxMessagesPerWindow = 20;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(120);

        // Per API instance, like the student assistant's limits.
        private static readonly ConcurrentDictionary<int, Queue<DateTime>> Recent = new();
        private static readonly ConcurrentDictionary<int, byte> Active = new();
        private static readonly SemaphoreSlim GlobalSlots = new(8, 8);

        private readonly IApplicationDbContext _context;
        private readonly IRunTokenService _runTokens;
        private readonly IN8nStreamClient _stream;

        public CompanyChatService(IApplicationDbContext context, IRunTokenService runTokens, IN8nStreamClient stream)
        {
            _context = context;
            _runTokens = runTokens;
            _stream = stream;
        }

        public async Task<ICareerChatSession> BeginAsync(
            int userId, CareerChatRequest request, CancellationToken cancellationToken = default)
        {
            var message = TextCleaner.Clean(request?.Message, MaxMessageChars, keepNewlines: true);
            if (message == null)
                throw new InvalidOperationException("Write a message first.");

            var history = CleanHistory(request!.History);

            var status = await _context.CompanyProfiles
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Select(c => (CompanyVerificationStatus?)c.VerificationStatus)
                .FirstOrDefaultAsync(cancellationToken);
            if (status == null)
                throw new InvalidOperationException("Company profile not found.");
            if (status == CompanyVerificationStatus.Suspended)
                throw new InvalidOperationException("The assistant is not available while your account is suspended.");

            var payload = new N8nCompanyChatRequest
            {
                Message = message,
                History = history,
                RunToken = _runTokens.Create(userId, "Company", RunTokenPurpose)
            };

            // Last step: from here on the caller must dispose the session to free the slot.
            Reserve(userId);

            return new Session(this, userId, payload);
        }

        private static List<N8nChatTurn> CleanHistory(List<CareerChatTurn>? turns)
        {
            var cleaned = new List<N8nChatTurn>();
            foreach (var turn in (turns ?? new List<CareerChatTurn>()).TakeLast(MaxHistoryTurns))
            {
                var role = turn?.Role?.Trim().ToLowerInvariant();
                if (role != "user" && role != "assistant") continue;

                var content = TextCleaner.Clean(turn!.Content, MaxTurnChars, keepNewlines: true);
                if (content == null) continue;

                cleaned.Add(new N8nChatTurn { Role = role, Content = content });
            }

            while (cleaned.Count > 0 && cleaned.Sum(t => t.Content.Length) > MaxHistoryChars)
                cleaned.RemoveAt(0);

            return cleaned;
        }

        private static void Reserve(int userId)
        {
            var queue = Recent.GetOrAdd(userId, _ => new Queue<DateTime>());
            lock (queue)
            {
                var now = DateTime.UtcNow;
                while (queue.Count > 0 && now - queue.Peek() > Window) queue.Dequeue();

                if (queue.Count >= MaxMessagesPerWindow)
                {
                    var wait = (int)Math.Ceiling((Window - (now - queue.Peek())).TotalSeconds);
                    throw new CareerChatLimitException(
                        "You have sent a lot of messages. Please wait a few minutes before the next one.", Math.Max(wait, 1));
                }

                if (!Active.TryAdd(userId, 0))
                    throw new CareerChatLimitException("Wait for the previous reply to finish.", 5);

                if (!GlobalSlots.Wait(0))
                {
                    Active.TryRemove(userId, out _);
                    throw new CareerChatLimitException("The assistant is busy right now. Please try again in a minute.", 60);
                }

                queue.Enqueue(now);
            }
        }

        private static void Release(int userId)
        {
            Active.TryRemove(userId, out _);
            GlobalSlots.Release();
        }

        private sealed class Session : ICareerChatSession
        {
            private readonly CompanyChatService _owner;
            private readonly int _userId;
            private readonly N8nCompanyChatRequest _payload;
            private int _released;

            public Session(CompanyChatService owner, int userId, N8nCompanyChatRequest payload)
            {
                _owner = owner;
                _userId = userId;
                _payload = payload;
            }

            public async IAsyncEnumerable<string> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ReplyTimeout);

                var tail = new StringBuilder();
                var total = 0;

                await foreach (var chunk in _owner._stream.StreamTextAsync(WorkflowPath, _payload, timeout.Token))
                {
                    if (string.IsNullOrEmpty(chunk)) continue;

                    // Never pass the run token on, even if the model repeated it.
                    tail.Append(chunk);
                    if (tail.Length > 600) tail.Remove(0, tail.Length - 600);
                    if (tail.ToString().Contains(_payload.RunToken, StringComparison.Ordinal))
                        throw new InvalidOperationException("The reply was stopped.");

                    total += chunk.Length;
                    if (total > MaxReplyChars) yield break;

                    yield return chunk;
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                    Release(_userId);
            }
        }
    }
}
