using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Participants;
using PharmaFlow.Domain.Participants.Events;
using PharmaFlow.Tests.Unit.Common.Helpers;

namespace PharmaFlow.Tests.Unit.Participants;

public class ParticipantTests
{
    private static readonly FrozenClock Clock = new(
        new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero)
    );

    private static Participant NewValidParticipant(ParticipantId? id = null) =>
        Participant.Create(
            id ?? ParticipantId.New(),
            siteId: SiteId.New(),
            subjectNumber: "S-001-024",
            yearOfBirth: 1990,
            sex: Sex.Female,
            initials: "ABC",
            clock: Clock
        ).Value;

    private static Participant ParticipantInScreening()
    {
        var p = NewValidParticipant();
        p.StartScreening(new DateOnly(2026, 5, 6), Clock);
        return p;
    }

    private static Participant ParticipantInConsented()
    {
        var p = ParticipantInScreening();
        p.Consent(Clock);
        return p;
    }

    private static Participant ParticipantInEnrolled()
    {
        var p = ParticipantInConsented();
        p.Enrol(new DateOnly(2026, 5, 7), Clock);
        return p;
    }

    private static Participant ParticipantInActive()
    {
        var p = ParticipantInEnrolled();
        p.Activate(Clock);
        return p;
    }

    // --- Factory: happy path ---

    [Fact]
    public void Create_returns_success_with_status_Prospective()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "S-001-024", 1990, Sex.Female, "ABC", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.Prospective, result.Value.EnrolmentStatus);
    }

    [Fact]
    public void Create_raises_ParticipantCreated_event()
    {
        var p = NewValidParticipant();

        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantCreated>(p.DomainEvents[0]);
    }

    [Fact]
    public void Create_accepts_null_Initials()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "S-001-024", 1990, Sex.Male, initials: null, clock: Clock);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Initials);
    }

    // --- Factory: validation failures ---

    [Fact]
    public void Create_rejects_empty_SiteId()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.Empty, "S-001-024", 1990, Sex.Female, "ABC", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("participant.site_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_invalid_SubjectNumber_format()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "ABC-123", 1990, Sex.Female, "ABC", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("participant.subject_number.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_YearOfBirth_below_1900()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "S-001-024", 1899, Sex.Female, "ABC", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("participant.year_of_birth.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_YearOfBirth_under_18_years_ago()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "S-001-024", 2020, Sex.Female, "ABC", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("participant.year_of_birth.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_lowercase_Initials()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "S-001-024", 1990, Sex.Female, "abc", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("participant.initials.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_4_char_Initials()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "S-001-024", 1990, Sex.Female, "ABCD", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("participant.initials.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_null_SubjectNumber_returns_Validation()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), subjectNumber: null!, 1990, Sex.Female, "ABC", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("participant.subject_number.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_whitespace_SubjectNumber_returns_Validation()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "   ", 1990, Sex.Female, "ABC", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("participant.subject_number.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_undefined_Sex()
    {
        var result = Participant.Create(
            ParticipantId.New(), SiteId.New(), "S-001-024", 1990, (Sex)99, "ABC", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("participant.sex.invalid", result.Error.Code);
    }

    // --- Lifecycle: happy path ---

    [Fact]
    public void StartScreening_from_Prospective_transitions_to_Screening()
    {
        var p = NewValidParticipant();
        p.ClearEvents();

        var result = p.StartScreening(new DateOnly(2026, 5, 6), Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.Screening, p.EnrolmentStatus);
        Assert.Equal(new DateOnly(2026, 5, 6), p.ScreeningDate);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantScreeningStarted>(p.DomainEvents[0]);
    }

    [Fact]
    public void FailScreening_from_Screening_transitions_to_ScreenFailed()
    {
        var p = ParticipantInScreening();
        p.ClearEvents();

        var result = p.FailScreening("ineligible per protocol", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.ScreenFailed, p.EnrolmentStatus);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantScreenFailed>(p.DomainEvents[0]);
    }

    [Fact]
    public void Consent_from_Screening_transitions_to_Consented()
    {
        var p = ParticipantInScreening();
        p.ClearEvents();

        var result = p.Consent(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.Consented, p.EnrolmentStatus);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantConsented>(p.DomainEvents[0]);
    }

    [Fact]
    public void Enrol_from_Consented_transitions_to_Enrolled()
    {
        var p = ParticipantInConsented();
        p.ClearEvents();

        var result = p.Enrol(new DateOnly(2026, 5, 7), Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.Enrolled, p.EnrolmentStatus);
        Assert.Equal(new DateOnly(2026, 5, 7), p.EnrolmentDate);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantEnrolled>(p.DomainEvents[0]);
    }

    [Fact]
    public void Activate_from_Enrolled_transitions_to_Active()
    {
        var p = ParticipantInEnrolled();
        p.ClearEvents();

        var result = p.Activate(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.Active, p.EnrolmentStatus);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantActivated>(p.DomainEvents[0]);
    }

    [Fact]
    public void Complete_from_Active_transitions_to_Completed()
    {
        var p = ParticipantInActive();
        p.ClearEvents();

        var result = p.Complete(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.Completed, p.EnrolmentStatus);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantCompleted>(p.DomainEvents[0]);
    }

    [Fact]
    public void Withdraw_from_Active_transitions_to_Withdrawn()
    {
        var p = ParticipantInActive();
        p.ClearEvents();

        var result = p.Withdraw(new DateOnly(2026, 5, 8), "subject request", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.Withdrawn, p.EnrolmentStatus);
        Assert.Equal(new DateOnly(2026, 5, 8), p.WithdrawalDate);
        Assert.Equal("subject request", p.WithdrawalReason);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantWithdrawn>(p.DomainEvents[0]);
    }

    [Fact]
    public void MarkLostToFollowUp_from_Active_transitions_to_LostToFollowUp()
    {
        var p = ParticipantInActive();
        p.ClearEvents();

        var result = p.MarkLostToFollowUp(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantStatus.LostToFollowUp, p.EnrolmentStatus);
        Assert.Single(p.DomainEvents);
        Assert.IsType<ParticipantLostToFollowUp>(p.DomainEvents[0]);
    }

    // --- Lifecycle: illegal transitions (assert ErrorType + Error.Code per PFL-020 carry-forward) ---

    [Fact]
    public void Enrol_from_Prospective_returns_Conflict()
    {
        var p = NewValidParticipant();

        var result = p.Enrol(new DateOnly(2026, 5, 7), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("participant.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void Withdraw_from_Screening_returns_Conflict()
    {
        var p = ParticipantInScreening();

        var result = p.Withdraw(new DateOnly(2026, 5, 8), "subject request", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
        Assert.Equal("participant.transition.invalid", result.Error.Code);
    }

    // --- Validation on lifecycle ---

    [Fact]
    public void FailScreening_with_empty_reason_returns_Validation()
    {
        var p = ParticipantInScreening();

        var result = p.FailScreening("  ", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }

    [Fact]
    public void Withdraw_with_empty_reason_returns_Validation()
    {
        var p = ParticipantInActive();

        var result = p.Withdraw(new DateOnly(2026, 5, 8), "  ", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
    }
}