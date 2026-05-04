using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies.Events;

namespace PharmaFlow.Domain.Studies;

public sealed class Study : Entity<StudyId>
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

    public static Result<Study> Create (
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
        if (string.IsNullOrWhiteSpace(protocolNumber) || protocolNumber.Length > 50)
        {
            return Error.Validation(
                "study.protocol_number.invalid",
                "Protocol number must be non-empty and ≤ 50 characters."
            );
        }

        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
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

        if (string.IsNullOrWhiteSpace(therapeuticArea) || therapeuticArea.Length > 200)
        {
            return Error.Validation(
                "study.therapeutic_area.invalid",
                "Therapeutic area must be non-empty and ≤ 100 characters."
            );
        }

        if (string.IsNullOrWhiteSpace(sponsorOrganization) || sponsorOrganization.Length > 200)
        {
            return Error.Validation(
                "study.sponsor.invalid",
                "Sponsor organization must be non-empty and ≤ 200 characters."
            );
        }

        if (plannedEnrolment <= 0)
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
}