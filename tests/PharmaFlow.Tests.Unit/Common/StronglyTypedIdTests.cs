using System.Reflection;

using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Tests.Unit.Common;

public class StronglyTypedIdTests
{
    [Fact]
    public void New_produces_non_empty_id()
    {
        var id = StudyId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_produces_unique_ids()
    {
        var a = StudyId.New();
        var b = StudyId.New();
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Empty_value_is_Guid_empty()
    {
        Assert.Equal(Guid.Empty, StudyId.Empty.Value);
    }

    [Fact]
    public void Default_value_is_empty_equivalent()
    {
        StudyId defaultId = default;
        Assert.Equal(Guid.Empty, defaultId.Value);
        Assert.Equal(StudyId.Empty, defaultId);
    }

    [Fact]
    public void Equality_by_value()
    {
        var g = Guid.NewGuid();
        Assert.Equal(new StudyId(g), new StudyId(g));
        Assert.True(new StudyId(g) == new StudyId(g));
        Assert.NotEqual(new StudyId(Guid.NewGuid()), new StudyId(Guid.NewGuid()));
    }

    [Fact]
    public void Different_id_types_with_same_guid_are_not_equal()
    {
        var g = Guid.NewGuid();
        var studyId = new StudyId(g);
        var userId = new UserId(g);
        Assert.False(studyId.Equals(userId));
        Assert.False(userId.Equals(studyId));
    }

    [Fact]
    public void AuditEventId_has_no_new()
    {
        var newMethod = typeof(AuditEventId).GetMethod(
            "New",
            BindingFlags.Public | BindingFlags.Static);
        Assert.Null(newMethod);
    }

    [Fact]
    public void AuditEventId_Empty_is_zero()
    {
        Assert.Equal(0L, AuditEventId.Empty.Value);
    }

    [Fact]
    public void ToString_formats_as_underlying_value()
    {
        var g = Guid.NewGuid();
        Assert.Equal(g.ToString(), new StudyId(g).ToString());
        Assert.Equal("42", new AuditEventId(42L).ToString());
    }
}