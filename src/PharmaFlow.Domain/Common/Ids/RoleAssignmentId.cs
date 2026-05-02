namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct RoleAssignmentId(Guid Value) : IStronglyTypedId<Guid>
{
    public static RoleAssignmentId New() => new(Guid.CreateVersion7());
    public static RoleAssignmentId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
}