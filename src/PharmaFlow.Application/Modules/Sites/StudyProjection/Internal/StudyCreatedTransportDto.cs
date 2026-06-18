namespace PharmaFlow.Application.Modules.Sites.StudyProjection.Internal;

public sealed record StudyCreatedTransportDto(
    Guid StudyId,
    DateTimeOffset OccurredAt,
    int Version
);