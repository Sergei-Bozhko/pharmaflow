namespace PharmaFlow.Application.Common.Idempotency;

public sealed class IdempotencyRecord
{
    public string Key { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public string RequestHash { get; private set; } = default!;
    public int ResponseStatus { get; private set; }
    public string ResponseBody { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }

    private IdempotencyRecord() { }

    private IdempotencyRecord(
        string key,
        Guid userId,
        string requestHash,
        int responseStatus,
        string responseBody,
        DateTimeOffset expiresAt
    )
    {
        Key = key;
        UserId = userId;
        RequestHash = requestHash;
        ResponseStatus = responseStatus;
        ResponseBody = responseBody;
        ExpiresAt = expiresAt;
    }

    // Create factory
    public static IdempotencyRecord Create(
        string key,
        Guid userId,
        string requestHash,
        int responseStatus,
        string responseBody,
        DateTimeOffset expiresAt
    )
    {
        return new IdempotencyRecord(
            key,
            userId,
            requestHash,
            responseStatus,
            responseBody,
            expiresAt
        );
    }
}