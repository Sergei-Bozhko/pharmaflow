using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Participants.Events;

public sealed record ParticipantScreenFailed(
    ParticipantId ParticipantId,
    string Reason,
    DateTimeOffset OccurredAt
) : IDomainEvent;