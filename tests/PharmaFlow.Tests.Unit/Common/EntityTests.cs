using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Tests.Unit.Common;

public class EntityTests
{
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

    private sealed class TestEntity : Entity<StudyId>
    {
        public TestEntity() : base() { }
        public TestEntity(StudyId id) : base(id) { }
    }
}