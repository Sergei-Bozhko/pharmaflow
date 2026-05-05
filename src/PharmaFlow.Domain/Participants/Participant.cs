using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Participants;

public sealed class Participant : Entity<ParticipantId>
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

    private Participant( ) { }

    private Participant(
        ParticipantId id,
        string subjectNumber,
        string? initials,
        int yearOfBirth,
        Sex sex,
        ParticipantStatus enrolmentStatus
    ) : base(id)
    {
        SubjectNumber = subjectNumber;
        Initials = initials;
        YearOfBirth = yearOfBirth;
        Sex = sex;
        EnrolmentStatus = enrolmentStatus;
    }
}