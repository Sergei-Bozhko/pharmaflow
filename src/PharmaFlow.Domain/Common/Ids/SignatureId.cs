namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct SignatureId(Guid Value) : IStronglyTypedId<Guid>
{
    public static SignatureId New() => new(Guid.CreateVersion7());
    public static SignatureId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
}