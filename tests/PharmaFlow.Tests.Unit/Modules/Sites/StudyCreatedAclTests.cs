using PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;
using PharmaFlow.Tests.Common;

namespace PharmaFlow.Tests.Unit.Modules.Sites;

// PFL-066 anti-corruption layer. The consumer translates the transport DTO into its OWN
// KnownStudy model — deliberately NOT a 1:1 copy: RegisteredAt is when WE learned of the
// study (the consumer's clock), not the producer's OccurredAt. The point is protection — a
// producer-side contract change is absorbed in this adapter, not rippled into Sites.
public class StudyCreatedAclTests
{
    private static readonly DateTimeOffset Occurred = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Maps_the_study_id_straight_through()
    {
        var studyId = Guid.NewGuid();

        var known = StudyCreatedAcl.ToKnownStudy(
            new StudyCreatedTransportDto(studyId, Occurred, 1), new FrozenClock(Now));

        Assert.Equal(studyId, known.StudyId);
    }

    [Fact]
    public void RegisteredAt_is_the_consumer_clock_not_the_wire_timestamp()
    {
        var known = StudyCreatedAcl.ToKnownStudy(
            new StudyCreatedTransportDto(Guid.NewGuid(), Occurred, 1), new FrozenClock(Now));

        Assert.Equal(Now, known.RegisteredAt);          // stamped when the consumer learned of it
        Assert.NotEqual(Occurred, known.RegisteredAt);  // NOT a 1:1 copy of the wire field
    }
}