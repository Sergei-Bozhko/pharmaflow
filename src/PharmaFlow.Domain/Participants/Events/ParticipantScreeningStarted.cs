using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Participants.Events;

public sealed record ParticipantScreeningStarted(
    ParticipantId ParticipantId,
    DateTimeOffset OccurredAt
) : IDomainEvent;