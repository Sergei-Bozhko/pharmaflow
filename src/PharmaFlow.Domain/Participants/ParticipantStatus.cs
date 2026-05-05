namespace PharmaFlow.Domain.Participants;

public enum ParticipantStatus
{
    Prospective = 0,
    Screening = 1,
    ScreenFailed = 2,
    Consented = 3,
    Enrolled = 4,
    Active = 5,
    Completed = 6,
    Withdrawn = 7,
    LostToFollowUp = 8,
}