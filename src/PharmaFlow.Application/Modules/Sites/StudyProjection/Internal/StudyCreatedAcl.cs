using PharmaFlow.Application.Modules.Sites.Internal;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;

public static class StudyCreatedAcl
{
    public static KnownStudy ToKnownStudy(StudyCreatedTransportDto transportDto, IClock clock) =>
        new(transportDto.StudyId, clock.UtcNow);
}