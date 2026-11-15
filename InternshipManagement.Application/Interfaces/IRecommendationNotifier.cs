namespace InternshipManagement.Application.Interfaces
{
    /// <summary>Tells students about newly published internships that fit their profile.</summary>
    public interface IRecommendationNotifier
    {
        /// <summary>Returns how many notifications were created in this run.</summary>
        Task<int> NotifyNewMatchesAsync(CancellationToken cancellationToken = default);
    }
}
