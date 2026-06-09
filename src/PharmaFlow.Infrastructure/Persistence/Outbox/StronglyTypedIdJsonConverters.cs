using System.Text.Json;
using System.Text.Json.Serialization;

using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Infrastructure.Persistence.Outbox;

/// <summary>
/// System.Text.Json sibling of <see cref="Conventions.StronglyTypedIdValueConverter{TId,TKey}"/>.
/// Serializes a strongly-typed id as its inner key (Guid -> string, long -> number),
/// so outbox payloads carry bare ids, not {"value":...} wrappers.
/// </summary>
public sealed class StronglyTypedIdJsonConverter<TId, TKey> : JsonConverter<TId>
    where TId : struct, IStronglyTypedId<TKey>
    where TKey : notnull
{
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = JsonSerializer.Deserialize<TKey>(ref reader, options)!;
        return (TId)Activator.CreateInstance(typeof(TId), value)!;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.Value, options);
}