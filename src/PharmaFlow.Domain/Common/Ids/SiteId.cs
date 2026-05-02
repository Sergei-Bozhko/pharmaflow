namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct SiteId(Guid Value) : IStronglyTypedId<Guid>
{
    public static SiteId New() => new(Guid.CreateVersion7());
    public static SiteId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
}