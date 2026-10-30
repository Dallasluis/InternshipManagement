namespace InternshipManagement.Application.Interfaces
{
    public interface IPasswordBreachChecker
    {
        Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken = default);
    }
}
