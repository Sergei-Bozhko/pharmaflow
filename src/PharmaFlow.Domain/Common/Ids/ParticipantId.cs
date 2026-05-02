namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct ParticipantId(Guid Value) : IStronglyTypedId<Guid>
{
    public static ParticipantId New() => new(Guid.CreateVersion7());
    public static ParticipantId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
}