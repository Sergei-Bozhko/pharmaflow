using System.Text.Json;
using System.Text.Json.Serialization;

using Mediator;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Infrastructure.Persistence.Outbox;

public static class OutboxSerialization
{
    // contract string -> CLR type. One line per event. Adding an event = adding a line.
    private static readonly Dictionary<string, Type> ByName = new()
    {
        ["StudyCreated"] = typeof(StudyCreatedIntegrationEvent),
    };

    private static readonly Dictionary<Type, string> ByType =
        ByName.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static readonly JsonSerializerOptions Options = BuildOptions();

    public static string NameOf(INotification notification) =>
        ByType.TryGetValue(notification.GetType(), out var name)
            ? name
            : throw new InvalidOperationException(
                $"Integration event {notification.GetType().Name} is not registered in OutboxSerialization. Add it to the map.");

    public static Type Resolve(string type) =>
        ByName.TryGetValue(type, out var t)
            ? t
            : throw new InvalidOperationException($"Unknown outbox event type '{type}'.");

    public static bool IsRegistered(Type eventType) => ByType.ContainsKey(eventType);

    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        o.Converters.Add(new JsonStringEnumConverter()); // Role/UserStatus as strings, matches EF HasConversion<string>()
        o.Converters.Add(new StronglyTypedIdJsonConverter<StudyId, Guid>());
        o.Converters.Add(new StronglyTypedIdJsonConverter<SiteId, Guid>());
        o.Converters.Add(new StronglyTypedIdJsonConverter<ParticipantId, Guid>());
        o.Converters.Add(new StronglyTypedIdJsonConverter<UserId, Guid>());
        o.Converters.Add(new StronglyTypedIdJsonConverter<RoleAssignmentId, Guid>());
        o.Converters.Add(new StronglyTypedIdJsonConverter<SignatureId, Guid>());
        return o;
    }
}