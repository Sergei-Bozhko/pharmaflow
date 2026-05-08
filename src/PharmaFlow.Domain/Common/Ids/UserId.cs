namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct UserId(Guid Value) : IStronglyTypedId<Guid>
{
    public static UserId New() => new(Guid.CreateVersion7());
    public static UserId Empty { get; } = new(Guid.Empty);
    public static UserId System { get; } = new(new Guid("00000000-0000-0000-0000-000000000001"));
    public override string ToString() => Value.ToString();
}