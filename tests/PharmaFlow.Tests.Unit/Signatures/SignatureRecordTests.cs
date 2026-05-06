using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Signatures;

namespace PharmaFlow.Tests.Unit.Signatures;

public class SignatureRecordTests
{
    private static readonly DateTimeOffset SignedAt =
        new(2026, 5, 6, 10, 0, 0, TimeSpan.Zero);

    private const string ValidHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static Result<SignatureRecord> NewValid(
        UserId? signerUserId = null,
        string targetEntityType = "Study",
        string targetEntityId = "abc-123",
        string targetVersionOrHash = "v1",
        string reasonStatement = "Investigator approval",
        string signaturePayloadHash = ValidHash) =>
        SignatureRecord.Create(
            signerUserId: signerUserId ?? UserId.New(),
            signedAt: SignedAt,
            meaning: SignatureMeaning.Approved,
            targetEntityType: targetEntityType,
            targetEntityId: targetEntityId,
            targetVersionOrHash: targetVersionOrHash,
            reasonStatement: reasonStatement,
            authenticationMethod: AuthenticationMethod.PasswordReentry,
            signaturePayloadHash: signaturePayloadHash,
            previousSignatureHash: null,
            clientIp: "10.0.0.1",
            userAgent: "Mozilla/5.0",
            mfaMethod: null,
            continuousSession: false,
            correlationId: null,
            signingKeyId: null
        );

    // --- Factory: happy path ---

    [Fact]
    public void Create_returns_success_with_all_fields_populated()
    {
        var signer = UserId.New();

        var result = SignatureRecord.Create(
            signerUserId: signer,
            signedAt: SignedAt,
            meaning: SignatureMeaning.Approved,
            targetEntityType: "Study",
            targetEntityId: "study-7",
            targetVersionOrHash: "v3",
            reasonStatement: "Sponsor approval of protocol v3",
            authenticationMethod: AuthenticationMethod.Totp,
            signaturePayloadHash: ValidHash,
            previousSignatureHash: null,
            clientIp: "192.168.1.1",
            userAgent: "Chrome/130",
            mfaMethod: "TOTP",
            continuousSession: true,
            correlationId: "corr-1",
            signingKeyId: "kv-key-v2"
        );

        Assert.True(result.IsSuccess);
        var sr = result.Value;
        Assert.NotEqual(SignatureId.Empty, sr.Id);
        Assert.Equal(signer, sr.SignerUserId);
        Assert.Equal(SignedAt, sr.SignedAt);
        Assert.Equal(SignatureMeaning.Approved, sr.Meaning);
        Assert.Equal("Study", sr.TargetEntityType);
        Assert.Equal("study-7", sr.TargetEntityId);
        Assert.Equal("v3", sr.TargetVersionOrHash);
        Assert.Equal("Sponsor approval of protocol v3", sr.ReasonStatement);
        Assert.Equal(AuthenticationMethod.Totp, sr.AuthenticationMethod);
        Assert.Equal(ValidHash, sr.SignaturePayloadHash);
        Assert.Null(sr.PreviousSignatureHash);
        Assert.Equal("192.168.1.1", sr.ClientIp);
        Assert.Equal("Chrome/130", sr.UserAgent);
        Assert.Equal("TOTP", sr.MfaMethod);
        Assert.True(sr.ContinuousSession);
        Assert.Equal("corr-1", sr.CorrelationId);
        Assert.Equal("kv-key-v2", sr.SigningKeyId);
    }

    // --- Factory: validation failures ---

    [Fact]
    public void Create_rejects_empty_SignerUserId()
    {
        var result = NewValid(signerUserId: UserId.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.signer_user_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_TargetEntityType()
    {
        var result = NewValid(targetEntityType: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.target_entity_type.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_TargetEntityId()
    {
        var result = NewValid(targetEntityId: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.target_entity_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_TargetVersionOrHash()
    {
        var result = NewValid(targetVersionOrHash: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.target_version_or_hash.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_ReasonStatement()
    {
        var result = NewValid(reasonStatement: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.reason_statement.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_empty_SignaturePayloadHash()
    {
        var result = NewValid(signaturePayloadHash: "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.signature_payload_hash.required", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_wrong_length_SignaturePayloadHash()
    {
        var result = NewValid(signaturePayloadHash: "abc123");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.signature_payload_hash.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_non_hex_SignaturePayloadHash()
    {
        var nonHex = new string('z', 64);

        var result = NewValid(signaturePayloadHash: nonHex);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Equal("signature_record.signature_payload_hash.invalid", result.Error.Code);
    }

    // --- Structure: append-only contract ---

    [Fact]
    public void SignatureRecord_has_no_public_setters()
    {
        var setters = typeof(SignatureRecord)
            .GetProperties()
            .Select(p => p.GetSetMethod(nonPublic: false))
            .Where(m => m is not null)
            .ToList();

        Assert.Empty(setters);
    }

    [Fact]
    public void SignatureRecord_does_not_inherit_Entity()
    {
        Assert.Equal(typeof(object), typeof(SignatureRecord).BaseType);
    }
}