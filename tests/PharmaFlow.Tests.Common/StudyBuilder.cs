using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Tests.Common;

/// <summary>
/// Canonical valid <see cref="Study"/> for tests. Defaults match the values the audit
/// trail tests assert (<c>TestProtocol</c> / <c>testTitle</c> / <see cref="StudyPhase.PhaseI"/>).
/// </summary>
public static class StudyBuilder
{
    /// <summary>Builds a valid <see cref="Study"/>; pass <paramref name="id"/> to control identity.</summary>
    public static Study Create(IClock clock, StudyId? id = null)
    {
        var plannedStart = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var plannedEnd = DateOnly.FromDateTime(clock.UtcNow.AddDays(90).UtcDateTime);

        var result = Study.Create(
            id ?? StudyId.New(),
            "TestProtocol",
            "testTitle",
            StudyPhase.PhaseI,
            "OncologyTest",
            "TestSponsor",
            100,
            plannedStart,
            plannedEnd,
            clock);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"StudyBuilder produced an invalid Study: {result.Error?.Message}");
        }

        return result.Value;
    }
}