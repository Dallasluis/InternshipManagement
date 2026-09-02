using InternshipManagement.Domain.Enums;
using Xunit;

namespace InternshipManagement.UnitTests;

public class ApplicationStatusFlowTests
{
    [Fact]
    public void ApplicationStatus_Contains_Interview_And_Offer_Stages()
    {
        Assert.Equal(ApplicationStatus.Shortlisted, ApplicationStatus.Shortlisted);
        Assert.Equal(ApplicationStatus.InterviewScheduled, ApplicationStatus.InterviewScheduled);
        Assert.Equal(ApplicationStatus.InterviewCompleted, ApplicationStatus.InterviewCompleted);
        Assert.Equal(ApplicationStatus.OfferMade, ApplicationStatus.OfferMade);
        Assert.Equal(ApplicationStatus.Accepted, ApplicationStatus.Accepted);
    }
}
