using System.Globalization;

namespace PharmaFlow.Domain.Common.Ids;

public readonly record struct AuditEventId(long Value) : IStronglyTypedId<long>
{
    public static AuditEventId Empty { get; } = new(0L);
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}