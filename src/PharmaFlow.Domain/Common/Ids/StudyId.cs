namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct StudyId(Guid Value) : IStronglyTypedId<Guid>
{
    public static StudyId New() => new(Guid.CreateVersion7());

    public static StudyId Empty { get; } = new(Guid.Empty);

    public override string ToString() => Value.ToString();
}