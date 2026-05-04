using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Sites;
using PharmaFlow.Domain.Sites.Events;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Sites;

public class SiteTests
{
    private static readonly FrozenClock Clock = new(
        new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero)
    );

    private static Site NewValidSite(SiteId? id = null) =>
        Site.Create(
            id ?? SiteId.New(),
            studyId: StudyId.New(),
            siteNumber: "S-001",
            name: "Berlin Charité",
            country: "DE",
            principalInvestigatorUserId: UserId.New(),
            clock: Clock
        ).Value;

    private static SignatureMeta ValidSignature(string reason = "Sponsor approval") =>
        new(SignatureId.New(), UserId.New(), Clock.UtcNow, reason);

    private static Site SiteInQualified()
    {
        var s = NewValidSite();
        s.Qualify();
        return s;
    }

    private static Site SiteInInitiated()
    {
        var s = SiteInQualified();
        s.Initiate();
        return s;
    }

    private static Site SiteInActive()
    {
        var s = SiteInInitiated();
        s.Activate(ValidSignature("Sponsor sign"), ValidSignature("PI sign"), Clock);
        return s;
    }

    // --- Factory: happy path ---

    [Fact]
    public void Create_returns_success_with_status_Selected()
    {
        var result = Site.Create(
            SiteId.New(), StudyId.New(), "S-001", "Berlin Charité", "DE", UserId.New(), Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(SiteStatus.Selected, result.Value.Status);
    }

    [Fact]
    public void Create_raises_SiteCreated_event()
    {
        var site = NewValidSite();

        Assert.Single(site.DomainEvents);
        Assert.IsType<SiteCreated>(site.DomainEvents[0]);
    }

    // --- Factory: validation failures ---

    [Fact]
    public void Create_rejects_empty_SiteNumber()
    {
        var result = Site.Create(
            SiteId.New(), StudyId.New(), "  ", "Berlin Charité", "DE", UserId.New(), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("site.number.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_invalid_Country_lowercase()
    {
        var result = Site.Create(
            SiteId.New(), StudyId.New(), "S-001", "Berlin Charité", "de", UserId.New(), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("site.country.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_invalid_Country_three_chars()
    {
        var result = Site.Create(
            SiteId.New(), StudyId.New(), "S-001", "Berlin Charité", "DEU", UserId.New(), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("site.country.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_StudyId()
    {
        var result = Site.Create(
            SiteId.New(), StudyId.Empty, "S-001", "Berlin Charité", "DE", UserId.New(), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("site.study_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_PI_UserId()
    {
        var result = Site.Create(
            SiteId.New(), StudyId.New(), "S-001", "Berlin Charité", "DE", UserId.Empty, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("site.pi_user_id.required", result.Error.Code);
    }

    // --- Lifecycle: happy path ---

    [Fact]
    public void Qualify_from_Selected_transitions_to_Qualified()
    {
        var site = NewValidSite();
        var result = site.Qualify();

        Assert.True(result.IsSuccess);
        Assert.Equal(SiteStatus.Qualified, site.Status);
    }

    [Fact]
    public void Initiate_from_Qualified_transitions_to_Initiated()
    {
        var site = SiteInQualified();
        var result = site.Initiate();

        Assert.True(result.IsSuccess);
        Assert.Equal(SiteStatus.Initiated, site.Status);
    }

    [Fact]
    public void Activate_from_Initiated_transitions_to_Active_and_sets_ActivationDate()
    {
        var site = SiteInInitiated();
        site.ClearEvents();

        var result = site.Activate(ValidSignature("Sponsor sign"), ValidSignature("PI sign"), Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(SiteStatus.Active, site.Status);
        Assert.Equal(Clock.UtcNow, site.ActivationDate);
        Assert.Single(site.DomainEvents);
        Assert.IsType<SiteActivated>(site.DomainEvents[0]);
    }

    [Fact]
    public void Close_from_Active_transitions_to_Closed_and_raises_SiteClosed()
    {
        var site = SiteInActive();
        site.ClearEvents();

        var result = site.Close(ValidSignature(), "trial complete", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(SiteStatus.Closed, site.Status);
        Assert.Single(site.DomainEvents);
        Assert.IsType<SiteClosed>(site.DomainEvents[0]);
    }

    // --- Lifecycle: illegal transitions ---

    [Fact]
    public void Activate_from_Selected_returns_Conflict()
    {
        var site = NewValidSite();

        var result = site.Activate(ValidSignature("Sponsor"), ValidSignature("PI"), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    [Fact]
    public void Close_from_Initiated_returns_Conflict()
    {
        var site = SiteInInitiated();

        var result = site.Close(ValidSignature(), "early stop", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    [Fact]
    public void Initiate_from_Selected_returns_Conflict()
    {
        var site = NewValidSite();

        var result = site.Initiate();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("site.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void Initiate_from_Active_returns_Conflict()
    {
        var site = SiteInActive();

        var result = site.Initiate();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("site.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void Qualify_from_Initiated_returns_Conflict()
    {
        var site = SiteInInitiated();

        var result = site.Qualify();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("site.transition.invalid", result.Error.Code);
    }

    // --- Validation on lifecycle ---

    [Fact]
    public void Activate_with_empty_sponsor_signature_reason_returns_Validation()
    {
        var site = SiteInInitiated();
        var sponsor = new SignatureMeta(SignatureId.New(), UserId.New(), Clock.UtcNow, "  ");
        var pi = ValidSignature("PI sign");

        var result = site.Activate(sponsor, pi, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }

    [Fact]
    public void Activate_with_empty_investigator_signature_reason_returns_Validation()
    {
        var site = SiteInInitiated();
        var sponsor = ValidSignature("Sponsor sign");
        var pi = new SignatureMeta(SignatureId.New(), UserId.New(), Clock.UtcNow, "  ");

        var result = site.Activate(sponsor, pi, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("site.activate.signature_reason_required", result.Error.Code);
    }

    [Fact]
    public void Close_with_empty_reason_returns_Validation()
    {
        var site = SiteInActive();

        var result = site.Close(ValidSignature(), "  ", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }
}