using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Participants.Events;

public sealed record ParticipantWithdrawn(
    ParticipantId ParticipantId,
    DateOnly WithdrawalDate,
    string Reason,
    DateTimeOffset OccurredAt
) : IDomainEvent;