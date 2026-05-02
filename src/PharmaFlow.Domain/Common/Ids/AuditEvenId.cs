using System.Globalization;

namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct AuditEvenId(long Value) : IStronglyTypedId<long>
{
    public static AuditEvenId Empty { get; } = new(0L);
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}