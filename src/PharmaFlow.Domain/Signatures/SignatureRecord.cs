using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Domain.Signatures;

public sealed class SignatureRecord
{
    public SignatureId Id { get; private set; }
    public UserId SignerUserId { get; private set; }
    public DateTimeOffset SignedAt { get; private set; }
    public SignatureMeaning Meaning { get; private set; }
    public string TargetEntityType { get; private set; } = default!;
    public string TargetEntityId { get; private set; } = default!;
    public string TargetVersionOrHash { get; private set; } = default!;
    public string ReasonStatement { get; private set; } = default!;
    public AuthenticationMethod AuthenticationMethod { get; private set; }
    public string SignaturePayloadHash { get; private set; } = default!;
    public string? PreviousSignatureHash { get; private set; } = default!;
    public string? ClientIp { get; private set; } = default!;
    public string? UserAgent { get; private set; } = default!;
    public string? MfaMethod { get; private set; } = default!;
    public bool ContinuousSession { get; private set; } = default!;
    public string? CorrelationId { get; private set; } = default!;
    public string? SigningKeyId { get; private set; } = default!;

    private SignatureRecord() { }

    private SignatureRecord(
        SignatureId id,
        UserId signerUserId,
        DateTimeOffset signedAt,
        SignatureMeaning meaning,
        string targetEntityType,
        string targetEntityId,
        string targetVersionOrHash,
        string reasonStatement,
        AuthenticationMethod authenticationMethod,
        string signaturePayloadHash,
        string? previousSignatureHash,
        string? clientIp,
        string? userAgent,
        string? mfaMethod,
        bool continuousSession,
        string? correlationId,
        string? signingKeyId)
    {
        Id = id;
        SignerUserId = signerUserId;
        SignedAt = signedAt;
        Meaning = meaning;
        TargetEntityType = targetEntityType;
        TargetEntityId = targetEntityId;
        TargetVersionOrHash = targetVersionOrHash;
        ReasonStatement = reasonStatement;
        AuthenticationMethod = authenticationMethod;
        SignaturePayloadHash = signaturePayloadHash;
        PreviousSignatureHash = previousSignatureHash;
        ClientIp = clientIp;
        UserAgent = userAgent;
        MfaMethod = mfaMethod;
        ContinuousSession = continuousSession;
        CorrelationId = correlationId;
        SigningKeyId = signingKeyId;
    }

    public static Result<SignatureRecord> Create(
        UserId signerUserId,
        DateTimeOffset signedAt,
        SignatureMeaning meaning,
        string targetEntityType,
        string targetEntityId,
        string targetVersionOrHash,
        string reasonStatement,
        AuthenticationMethod authenticationMethod,
        string signaturePayloadHash,
        string? previousSignatureHash,
        string? clientIp,
        string? userAgent,
        string? mfaMethod,
        bool continuousSession,
        string? correlationId,
        string? signingKeyId
    )
    {
        if (signerUserId == UserId.Empty)
        {
            return Error.Validation(
                "signature_record.signer_user_id.required",
                "SignerUserId is required."
            );
        }

        if (string.IsNullOrWhiteSpace(targetEntityType))
        {
            return Error.Validation(
                "signature_record.target_entity_type.required",
                "TargetEntityType must be non-empty string."
            );
        }

        if (string.IsNullOrWhiteSpace(targetEntityId))
        {
            return Error.Validation(
                "signature_record.target_entity_id.required",
                "TargetEntityId must be non-empty string."
            );
        }

        if (string.IsNullOrWhiteSpace(targetVersionOrHash))
        {
            return Error.Validation(
                "signature_record.target_version_or_hash.required",
                "TargetVersionOrHash must be non-empty string."
            );
        }

        if (string.IsNullOrWhiteSpace(reasonStatement))
        {
            return Error.Validation(
                "signature_record.reason_statement.required",
                "ReasonStatement is required (21 CFR Part 11 §11.50)."
            );
        }

        if (string.IsNullOrWhiteSpace(signaturePayloadHash))
        {
            return Error.Validation(
                "signature_record.signature_payload_hash.required",
                "SignaturePayloadHash must be non-empty string."
            );
        }

        if (signaturePayloadHash.Length != 64 ||
            !signaturePayloadHash.All(char.IsAsciiHexDigit))
        {
            return Error.Validation(
                "signature_record.signature_payload_hash.invalid",
                "SignaturePayloadHash must be 64-char hex."
            );
        }

        var signatureRecord = new SignatureRecord(
            SignatureId.New(),
            signerUserId,
            signedAt,
            meaning,
            targetEntityType,
            targetEntityId,
            targetVersionOrHash,
            reasonStatement,
            authenticationMethod,
            signaturePayloadHash,
            previousSignatureHash,
            clientIp,
            userAgent,
            mfaMethod,
            continuousSession,
            correlationId,
            signingKeyId
        );
        return signatureRecord;
    }
}