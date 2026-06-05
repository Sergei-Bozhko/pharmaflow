using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Tests.Unit.Common;

public class EntityTests
{
    [Fact]
    public void Raise_adds_event_to_DomainEvents()
    {
        var entity = new TestEntity();
        entity.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));
        Assert.Single(entity.DomainEvents);
    }

    [Fact]
    public void Raise_appends_in_order()
    {
        var entity = new TestEntity();
        var first = new TestEvent(DateTimeOffset.UtcNow);
        var second = new TestEvent(DateTimeOffset.UtcNow.AddSeconds(1));

        entity.RaisePublic(first);
        entity.RaisePublic(second);

        Assert.Equal(2, entity.DomainEvents.Count);
        Assert.Same(first, entity.DomainEvents[0]);
        Assert.Same(second, entity.DomainEvents[1]);
    }

    [Fact]
    public void ClearEvents_empties_the_list()
    {
        var entity = new TestEntity();
        entity.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));
        entity.RaisePublic(new TestEvent(DateTimeOffset.UtcNow));

        entity.DequeueEvents();

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void Default_IsDeleted_is_false()
    {
        var entity = new TestEntity();
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void Two_entities_with_same_id_are_equal()
    {
        var id = StudyId.New();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Two_entities_with_different_Id_are_not_equal()
    {
        var a = new TestEntity(StudyId.New());
        var b = new TestEntity(StudyId.New());

        Assert.False(a.Equals(b));
        Assert.False(a == b);
    }

    [Fact]
    public void Entity_compared_to_null_is_not_equal()
    {
        var entity = new TestEntity();
        TestEntity? nullEntity = null;

        Assert.False(entity.Equals(nullEntity));
        Assert.False(entity == nullEntity);
        Assert.False(nullEntity == entity);
    }

    [Fact]
    public void DomainEvents_is_readonly()
    {
        var entity = new TestEntity();
        Assert.IsAssignableFrom<IReadOnlyList<IDomainEvent>>(entity.DomainEvents);
    }

    private sealed class TestEntity : AggregateRoot<StudyId>
    {
        public TestEntity() : base() { }
        public TestEntity(StudyId id) : base(id) { }
        public void RaisePublic(IDomainEvent e) => Raise(e);
    }

    private sealed record TestEvent(DateTimeOffset OccurredAt) : IDomainEvent;

}