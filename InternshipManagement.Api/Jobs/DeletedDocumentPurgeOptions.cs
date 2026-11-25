namespace InternshipManagement.Api.Jobs;

public sealed class DeletedDocumentPurgeOptions
{
    /// <summary>
    /// Number of days a document must remain marked Deleted before its file and metadata are purged.
    /// Null disables automatic purging until a retention period is approved and configured.
    /// </summary>
    public int? AfterDays { get; set; }
}
