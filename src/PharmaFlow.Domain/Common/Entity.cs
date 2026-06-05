namespace PharmaFlow.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>, IAuditedEntity
    where TId : struct
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTimeOffset UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public uint RowVersion { get; private set; }
    public bool IsDeleted { get; private set; }

    protected Entity() { }
    protected Entity(TId id) { Id = id; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> e && Equals(e);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b) =>
        a is null ? b is null : a.Equals(b);
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);
}