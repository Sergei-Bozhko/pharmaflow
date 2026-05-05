using System.Text.RegularExpressions;

using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Participants.Events;
using PharmaFlow.Domain.Sites;

namespace PharmaFlow.Domain.Participants;

public sealed partial class Participant : Entity<ParticipantId>
{
    public SiteId SiteId { get; private set; }
    public string SubjectNumber { get; private set; } = default!;
    public string? Initials { get; private set; }
    public int YearOfBirth { get; private set; }
    public Sex Sex { get; private set; }
    public ParticipantStatus EnrolmentStatus { get; private set; }
    public DateOnly? ScreeningDate { get; private set; }
    public DateOnly? EnrolmentDate { get; private set; }
    public DateOnly? WithdrawalDate { get; private set; }
    public string? WithdrawalReason { get; private set; }

    private Participant() { }

    private Participant(
        ParticipantId id,
        SiteId siteId,
        string subjectNumber,
        string? initials,
        int yearOfBirth,
        Sex sex,
        DateOnly? enrolmentDate,
        ParticipantStatus enrolmentStatus
    ) : base(id)
    {
        SiteId = siteId;
        SubjectNumber = subjectNumber;
        Initials = initials;
        YearOfBirth = yearOfBirth;
        Sex = sex;
        EnrolmentStatus = enrolmentStatus;
        EnrolmentDate = enrolmentDate;
    }

    public static Result<Participant> Create(
        ParticipantId id,
        SiteId siteId,
        string subjectNumber,
        int yearOfBirth,
        Sex sex,
        string? initials,
        IClock clock)
    {
        if (siteId != SiteId.Empty)
        {
            return Error.Validation(
                "participant.site_id.required",
                "SiteId must be non-empty."
            );
        }

        if (!SubjectNumberRegex().IsMatch(subjectNumber))
        {
            return Error.Validation(
                "participant.subject_number.invalid",
                "Subject number must follow S-XXX-XXX format, where X - any number."
            );
        }

        if (yearOfBirth < 1900 || yearOfBirth > clock.UtcNow.Year - 18)
        {
            return Error.Validation(
                "participant.year_of_birth.invalid",
                "Participant must be older then 18. Can't use year < 1900."
            );
        }

        if (!Enum.IsDefined(sex))
        {
            return Error.Validation(
                "participant.sex.invalid",
                "Participant sex is not defined."
            );
        }

        if (initials is not null &&
            initials.Length > 3 &&
            !initials.All(char.IsAsciiLetterUpper)
            )
        {
            return Error.Validation(
                "participant.initials.invalid",
                "Participant initials is not valid."
            );

        }

        var participant = new Participant(
            id,
            siteId,
            subjectNumber,
            initials,
            yearOfBirth,
            sex,
            DateOnly.FromDateTime(clock.UtcNow.Date),
            ParticipantStatus.Prospective
        );
        participant.Raise(new ParticipantCreated(id, clock.UtcNow));
        return participant;
    }

    public Result StartScreening(DateOnly screeningDate, IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Prospective)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot start screening a participant with status {EnrolmentStatus}."
            );
        }

        EnrolmentStatus = ParticipantStatus.Screening;
        ScreeningDate = screeningDate;
        Raise(new ParticipantScreeningStarted(Id, clock.UtcNow));
        return Result.Success();
    }

    public Result FailScreening(string reason, IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Screening)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot fail screening a participant with status {EnrolmentStatus}."
            );
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation(
                "participant.failscreening.reason_required",
                "Reason must be non-empty string."
            );
        }

        EnrolmentStatus = ParticipantStatus.ScreenFailed;
        Raise(new ParticipantScreenFailed(Id, reason, clock.UtcNow));
        return Result.Success();
    }

    public Result Consent(IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Screening)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot consent a participant with status {EnrolmentStatus}."
            );
        }
        EnrolmentStatus = ParticipantStatus.Consented;
        Raise(new ParticipantConsented(Id, clock.UtcNow));
        return Result.Success();
    }

    public Result Enrol(DateOnly enrolmentDate, IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Consented)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot enrol a participant with status {EnrolmentStatus}."
            );
        }
        EnrolmentStatus = ParticipantStatus.Enrolled;
        EnrolmentDate = enrolmentDate;
        Raise(new ParticipantEnrolled(Id, clock.UtcNow));
        return Result.Success();
    }

    public Result Activate(IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Enrolled)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot activate a participant with status {EnrolmentStatus}."
            );
        }
        EnrolmentStatus = ParticipantStatus.Active;
        Raise(new ParticipantActivated(Id, clock.UtcNow));
        return Result.Success();
    }

    public Result Complete(IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Active)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot complete a participant with status {EnrolmentStatus}."
            );
        }
        EnrolmentStatus = ParticipantStatus.Completed;
        Raise(new ParticipantCompleted(Id, clock.UtcNow));
        return Result.Success();
    }

    public Result Withdraw(DateOnly withdrawalDate, string reason, IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Active)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot withdraw a participant with status {EnrolmentStatus}."
            );
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation(
                "participant.withdraw.reason_required",
                "Reason must be non-empty string."
            );
        }

        EnrolmentStatus = ParticipantStatus.Withdrawn;
        WithdrawalReason = reason;
        Raise(new ParticipantWithdrawn(Id, withdrawalDate, reason, clock.UtcNow));
        return Result.Success();
    }

    public Result MarkLostToFollowUp(IClock clock)
    {
        if (EnrolmentStatus != ParticipantStatus.Active)
        {
            return Error.Conflict(
                "participant.transition.invalid",
                $"Cannot mark lost to follow up a participant with status {EnrolmentStatus}."
            );
        }

        EnrolmentStatus = ParticipantStatus.LostToFollowUp;
        Raise(new ParticipantLostToFollowUp(Id, clock.UtcNow));
        return Result.Success();
    }

    [GeneratedRegex("^S-\\d{3}-\\d{3}$")]
    private static partial Regex SubjectNumberRegex();
}