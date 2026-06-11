namespace PharmaFlow.Application.Modules.Sites.Internal;

public sealed partial class KnownStudy
{
    public Guid StudyId { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    private KnownStudy() { }

    public KnownStudy(Guid studyId, DateTimeOffset registeredAt)
    {
        StudyId = studyId;
        RegisteredAt = registeredAt;
    }
}