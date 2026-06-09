using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;
using PharmaFlow.Domain.Studies.Events;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Studies;

public class StudyTests
{
    private static readonly FrozenClock Clock = new(
        new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero)
    );

    private static readonly DateOnly StartDate = new(2026, 6, 1);
    private static readonly DateOnly EndDate = new(2027, 6, 1);

    private static Study NewValidStudy(StudyId? id = null) =>
        Study.Create(
            id ?? StudyId.New(),
            protocolNumber: "PROTO-001",
            title: "Test study",
            phase: StudyPhase.PhaseII,
            therapeuticArea: "Oncology",
            sponsorOrganization: "Takeda Pharmaceuticals",
            plannedEnrolment: 100,
            plannedStartDate: StartDate,
            plannedEndDate: EndDate,
            clock: Clock
        ).Value;

    private static SignatureMeta ValidSignature() =>
        new(SignatureId.New(), UserId.New(), Clock.UtcNow, "Sponsor approval");

    private static Study StudyInPendingApproval()
    {
        var s = NewValidStudy();
        s.SubmitForApproval();
        return s;
    }

    private static Study StudyInActive()
    {
        var s = StudyInPendingApproval();
        s.Activate(ValidSignature(), Clock);
        return s;
    }

    private static Study StudyInSuspended()
    {
        var s = StudyInActive();
        s.Suspend(ValidSignature(), "needed pause", Clock);
        return s;
    }

    private static Study StudyInClosed()
    {
        var s = StudyInSuspended();
        s.Close(ValidSignature(), "study complete", Clock);
        return s;
    }

    // --- Factory: happy path ---

    [Fact]
    public void Create_returns_success_with_status_draft()
    {
        var result = Study.Create(
            StudyId.New(),
            "PROTO-001",
            "Test study",
            StudyPhase.PhaseII,
            "Oncology",
            "Takeda Pharmaceuticals",
            100,
            StartDate,
            EndDate,
            Clock
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.Draft, result.Value.Status);
    }

    [Fact]
    public void Create_raises_StudyCreated_event()
    {
        var study = NewValidStudy();

        Assert.Single(study.DomainEvents);
        Assert.IsType<StudyCreated>(study.DomainEvents[0]);
    }

    [Fact]
    public void Create_assignes_provided_StudyId()
    {
        var id = StudyId.New();
        var study = NewValidStudy(id);

        Assert.Equal(id, study.Id);
    }

    // --- Factory: validation failures ---

    [Fact]
    public void Create_rejects_empty_ProtocolNumber()
    {
        var result = Study.Create(
            StudyId.New(), "  ", "Test Study", StudyPhase.PhaseII,
            "Oncology", "ACME Pharma", 100, StartDate, EndDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.protocol_number.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_too_long_ProtocolNumber()
    {
        var result = Study.Create(
            StudyId.New(), new string('P', 51), "Test Study", StudyPhase.PhaseII,
            "Oncology", "ACME Pharma", 100, StartDate, EndDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.protocol_number.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_Title()
    {
        var result = Study.Create(
            StudyId.New(), "PROTO-001", "  ", StudyPhase.PhaseII,
            "Oncology", "ACME Pharma", 100, StartDate, EndDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.title.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_invalid_Phase()
    {
        var result = Study.Create(
            StudyId.New(), "PROTO-001", "Test Study", (StudyPhase)99,
            "Oncology", "ACME Pharma", 100, StartDate, EndDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.phase.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_zero_PlannedEnrolment()
    {
        var result = Study.Create(
            StudyId.New(), "PROTO-001", "Test Study", StudyPhase.PhaseII,
            "Oncology", "ACME Pharma", 0, StartDate, EndDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.planned_enrolment.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_negative_PlannedEnrolment()
    {
        var result = Study.Create(
            StudyId.New(), "PROTO-001", "Test Study", StudyPhase.PhaseII,
            "Oncology", "ACME Pharma", -1, StartDate, EndDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.planned_enrolment.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_PlannedEndDate_equal_to_StartDate()
    {
        var result = Study.Create(
            StudyId.New(), "PROTO-001", "Test Study", StudyPhase.PhaseII,
            "Oncology", "ACME Pharma", 100, StartDate, StartDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.planned_dates.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_PlannedEndDate_before_StartDate()
    {
        var result = Study.Create(
            StudyId.New(), "PROTO-001", "Test Study", StudyPhase.PhaseII,
            "Oncology", "ACME Pharma", 100, EndDate, StartDate, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.planned_dates.invalid", result.Error.Code);
    }

    // --- Lifecycle: happy path ---                                                                                                       

    [Fact]
    public void SubmitForApproval_from_Draft_transitions_to_PendingApproval()
    {
        var study = NewValidStudy();
        var result = study.SubmitForApproval();

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.PendingApproval, study.Status);
    }

    [Fact]
    public void Activate_from_PendingApproval_transitions_to_Active_and_raises_StudyActivated()
    {
        var study = StudyInPendingApproval();
        study.DequeueEvents();

        var result = study.Activate(ValidSignature(), Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.Active, study.Status);
        Assert.Single(study.DomainEvents);
        Assert.IsType<StudyActivated>(study.DomainEvents[0]);
    }

    [Fact]
    public void Suspend_from_Active_transitions_to_Suspended_and_raises_StudySuspended()
    {
        var study = StudyInActive();
        study.DequeueEvents();

        var result = study.Suspend(ValidSignature(), "safety review", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.Suspended, study.Status);
        Assert.Single(study.DomainEvents);
        Assert.IsType<StudySuspended>(study.DomainEvents[0]);
    }

    [Fact]
    public void Close_from_Active_transitions_to_Closed_and_raises_StudyClosed()
    {
        var study = StudyInActive();
        study.DequeueEvents();

        var result = study.Close(ValidSignature(), "study complete", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.Closed, study.Status);
        Assert.Single(study.DomainEvents);
        Assert.IsType<StudyClosed>(study.DomainEvents[0]);
    }

    [Fact]
    public void Close_from_Suspended_transitions_to_Closed()
    {
        var study = StudyInSuspended();

        var result = study.Close(ValidSignature(), "abandoned post-suspension", Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.Closed, study.Status);
    }

    [Fact]
    public void Archive_from_Closed_transitions_to_Archived_and_raises_StudyArchived()
    {
        var study = StudyInClosed();
        study.DequeueEvents();

        var result = study.Archive(Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudyStatus.Archived, study.Status);
        Assert.Single(study.DomainEvents);
        Assert.IsType<StudyArchived>(study.DomainEvents[0]);
    }

    // --- Lifecycle: illegal transitions ---

    [Fact]
    public void Activate_from_Draft_returns_Conflict()
    {
        var study = NewValidStudy();

        var result = study.Activate(ValidSignature(), Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    [Fact]
    public void Suspend_from_PendingApproval_returns_Conflict()
    {
        var study = StudyInPendingApproval();

        var result = study.Suspend(ValidSignature(), "x", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    [Fact]
    public void Archive_from_Active_returns_Conflict()
    {
        var study = StudyInActive();

        var result = study.Archive(Clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    // --- Validation on lifecycle ---                                                                                                     

    [Fact]
    public void Suspend_with_empty_reason_returns_Validation()
    {
        var study = StudyInActive();

        var result = study.Suspend(ValidSignature(), "  ", Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.suspend.reason_required", result.Error.Code);
    }

    [Fact]
    public void Activate_with_empty_signature_reason_returns_Validation()
    {
        var study = StudyInPendingApproval();
        var sig = new SignatureMeta(SignatureId.New(), UserId.New(), Clock.UtcNow, "  ");

        var result = study.Activate(sig, Clock);

        Assert.True(result.IsFailure);
        Assert.Equal("study.activate.signature_reason_required", result.Error.Code);
    }
}