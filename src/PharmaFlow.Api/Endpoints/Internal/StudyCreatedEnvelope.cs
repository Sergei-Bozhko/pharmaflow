namespace PharmaFlow.Api.Endpoints.Internal;

public sealed record StudyCreatedEnvelope
(
    Guid MessageId,
    string Type,
    string Payload
);