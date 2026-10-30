// InternshipManagement.Application/Interfaces/IN8nClient.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces;

/// <summary>
/// The only way application code talks to n8n. Business rules stay in the API;
/// n8n workflows are called with a payload and return a response that MUST be validated by the caller.
/// </summary>
public interface IN8nClient
{
    Task<TResponse> RunWorkflowAsync<TRequest, TResponse>(
        string webhookPath, TRequest payload, CancellationToken cancellationToken = default);
}

public sealed class N8nException : Exception
{
    public N8nException(string message, Exception? inner = null) : base(message, inner) { }
}
