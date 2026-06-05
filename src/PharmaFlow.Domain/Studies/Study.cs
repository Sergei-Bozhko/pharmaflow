using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies.Events;

namespace PharmaFlow.Domain.Studies;

public sealed class Study : AggregateRoot<StudyId>
{
    public string ProtocolNumber { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public StudyPhase Phase { get; private set; }
    public string TherapeuticArea { get; private set; } = default!;
    public string SponsorOrganization { get; private set; } = default!;
    public int PlannedEnrolment { get; private set; }
    public DateOnly PlannedStartDate { get; private set; }
    public DateOnly PlannedEndDate { get; private set; }
    public StudyStatus Status { get; private set; }

    private Study() { }

    private Study(
        StudyId id,
        string protocolNumber,
        string title,
        StudyPhase phase,
        string therapeuticArea,
        string sponsorOrganization,
        int plannedEnrolment,
        DateOnly plannedStartDate,
        DateOnly plannedEndDate
    ) : base(id)
    {
        ProtocolNumber = protocolNumber;
        Title = title;
        Phase = phase;
        TherapeuticArea = therapeuticArea;
        SponsorOrganization = sponsorOrganization;
        PlannedEnrolment = plannedEnrolment;
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
        Status = StudyStatus.Draft;
    }

    public const int MaxProtocolNumberLength = 50;
    public const int MaxTitleLength = 200;
    public const int MaxTherapeuticAreaLength = 100;
    public const int MaxSponsorOrganizationLength = 200;
    public const int MinPlannedEnrolment = 1;

    public static Result<Study> Create(
        StudyId id,
        string protocolNumber,
        string title,
        StudyPhase phase,
        string therapeuticArea,
        string sponsorOrganization,
        int plannedEnrolment,
        DateOnly plannedStartDate,
        DateOnly plannedEndDate,
        IClock clock
    )
    {
        if (string.IsNullOrWhiteSpace(protocolNumber) || protocolNumber.Length > MaxProtocolNumberLength)
        {
            return Error.Validation(
                "study.protocol_number.invalid",
                "Protocol number must be non-empty and ≤ 50 characters."
            );
        }

        if (string.IsNullOrWhiteSpace(title) || title.Length > MaxTitleLength)
        {
            return Error.Validation(
                "study.title.invalid",
                "Title must be non-empty and ≤ 200 characters."
            );
        }

        if (!Enum.IsDefined(phase))
        {
            return Error.Validation(
                "study.phase.invalid",
                "Phase is not defined StudyPhase value."
            );
        }

        if (string.IsNullOrWhiteSpace(therapeuticArea) || therapeuticArea.Length > MaxTherapeuticAreaLength)
        {
            return Error.Validation(
                "study.therapeutic_area.invalid",
                "Therapeutic area must be non-empty and ≤ 100 characters."
            );
        }

        if (string.IsNullOrWhiteSpace(sponsorOrganization) || sponsorOrganization.Length > MaxSponsorOrganizationLength)
        {
            return Error.Validation(
                "study.sponsor.invalid",
                "Sponsor organization must be non-empty and ≤ 200 characters."
            );
        }

        if (plannedEnrolment < MinPlannedEnrolment)
        {
            return Error.Validation(
                "study.planned_enrolment.invalid",
                "Planned enrolment must be greater than zero."
            );
        }

        if (plannedEndDate <= plannedStartDate)
        {
            return Error.Validation(
                "study.planned_dates.invalid",
                "Planned end date must be strictly after start date."
            );
        }

        var study = new Study(
            id,
            protocolNumber,
            title,
            phase,
            therapeuticArea,
            sponsorOrganization,
            plannedEnrolment,
            plannedStartDate,
            plannedEndDate
        );

        study.Raise(new StudyCreated(id, clock.UtcNow));
        return study;
    }

    public Result SubmitForApproval()
    {
        if (Status != StudyStatus.Draft)
        {
            return Error.Conflict(
                "study.transition.invalid",
                $"Cannot submit a Study with status {Status} for approval."
            );
        }

        Status = StudyStatus.PendingApproval;
        return Result.Success();
    }

    public Result Activate(SignatureMeta signature, IClock clock)
    {
        if (Status != StudyStatus.PendingApproval)
        {
            return Error.Conflict(
                "study.transition.invalid",
                $"Cannot activate a Study with status {Status}."
            );
        }

        if (string.IsNullOrWhiteSpace(signature.Reason))
        {
            return Error.Validation(
                "study.activate.signature_reason_required",
                "Activation signature must include a non-empty reason."
            );
        }

        Status = StudyStatus.Active;
        Raise(new StudyActivated(Id, signature, clock.UtcNow));
        return Result.Success();
    }

    public Result Suspend(SignatureMeta signature, string reason, IClock clock)
    {
        if (Status != StudyStatus.Active)
        {
            return Error.Conflict(
                "study.transition.invalid",
                $"Cannot suspend a Study with status {Status}."
            );
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation(
                "study.suspend.reason_required",
                "Suspension reason must be non-empty."
            );
        }

        if (string.IsNullOrWhiteSpace(signature.Reason))
        {
            return Error.Validation(
                "study.suspend.signature_reason_required",
                "Suspension signature must include a non-empty reason."
            );
        }

        Status = StudyStatus.Suspended;
        Raise(new StudySuspended(Id, reason, signature, clock.UtcNow));
        return Result.Success();
    }

    public Result Close(SignatureMeta signature, string reason, IClock clock)
    {
        if (Status != StudyStatus.Active && Status != StudyStatus.Suspended)
        {
            return Error.Conflict(
                "study.transition.invalid",
                $"Cannot close a Study with status {Status}."
            );
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation(
                "study.close.reason_required",
                "Closure reason must be non-empty."
            );
        }

        if (string.IsNullOrWhiteSpace(signature.Reason))
        {
            return Error.Validation(
                "study.close.signature_reason_required",
                "Closure signature must include a non-empty reason."
            );
        }

        Status = StudyStatus.Closed;
        Raise(new StudyClosed(Id, reason, signature, clock.UtcNow));
        return Result.Success();
    }

    public Result Archive(IClock clock)
    {
        if (Status != StudyStatus.Closed)
        {
            return Error.Conflict(
                "study.transition.invalid",
                $"Cannot archive a Study with status {Status}."
            );
        }

        Status = StudyStatus.Archived;
        Raise(new StudyArchived(Id, clock.UtcNow));
        return Result.Success();
    }
}