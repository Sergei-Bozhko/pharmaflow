namespace PharmaFlow.Application.Common.Idempotency;

public interface IIdempotencyKeyProvider
{
    string? GetKey();
}