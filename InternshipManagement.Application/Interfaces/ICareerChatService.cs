using InternshipManagement.Application.DTOs.Student;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    /// <summary>Too many messages, a reply is still being written, or the server is busy. Maps to 429.</summary>
    public sealed class CareerChatLimitException : Exception
    {
        public int RetryAfterSeconds { get; }
        public CareerChatLimitException(string message, int retryAfterSeconds = 30) : base(message)
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }

    /// <summary>Streams the text of an n8n workflow's reply as it is written. Implemented in Infrastructure.</summary>
    public interface IN8nStreamClient
    {
        /// <exception cref="N8nException">n8n is unreachable, answered with an error, or sent nothing usable.</exception>
        IAsyncEnumerable<string> StreamTextAsync<TRequest>(
            string webhookPath, TRequest payload, CancellationToken cancellationToken = default);
    }

    /// <summary>One chat turn in progress. Disposing it frees the student's slot, so always dispose it.</summary>
    public interface ICareerChatSession : IDisposable
    {
        IAsyncEnumerable<string> StreamAsync(CancellationToken cancellationToken = default);
    }

    public interface ICareerChatService
    {
        /// <summary>
        /// Validates the message and history, applies the limits and prepares the turn. Nothing is sent to the AI
        /// until StreamAsync is read. Nothing is stored.
        /// </summary>
        /// <exception cref="InvalidOperationException">Bad input or no student profile (maps to 400).</exception>
        /// <exception cref="CareerChatLimitException">Limits reached (maps to 429).</exception>
        Task<ICareerChatSession> BeginAsync(int userId, CareerChatRequest request, CancellationToken cancellationToken = default);
    }

    public interface ICareerAgentToolsService
    {
        Task<StudentCareerSummary> GetProfileSummaryAsync(int userId, CancellationToken cancellationToken = default);
    }
}
