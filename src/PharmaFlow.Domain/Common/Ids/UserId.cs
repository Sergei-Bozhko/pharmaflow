namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct UserId(Guid Value) : IStronglyTypedId<Guid>
{
    public static UserId New() => new(Guid.CreateVersion7());
    public static UserId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
}