using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Common;

public sealed record SignatureMeta(
    SignatureId Id,
    UserId SignerUserId,
    DateTimeOffset SignedAt,
    string Reason
);