namespace PharmaFlow.Application.Modules.Sites.Internal;

public sealed partial class KnownStudy
{
    public Guid StudyId { get; private set; }
    public DateTimeOffset RegistredAt { get; private set; }

    private KnownStudy() { }

    public KnownStudy(Guid studyId, DateTimeOffset registredAt)
    {
        StudyId = studyId;
        RegistredAt = registredAt;
    }
}