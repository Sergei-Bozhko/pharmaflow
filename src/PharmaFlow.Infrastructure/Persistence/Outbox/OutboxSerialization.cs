using System.Text.Json;
using System.Text.Json.Serialization;

using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Participants.Events;
using PharmaFlow.Domain.Sites.Events;
using PharmaFlow.Domain.Studies.Events;
using PharmaFlow.Domain.Users.Events;

namespace PharmaFlow.Infrastructure.Persistence.Outbox;

public static class OutboxSerialization
{
    // contract string -> CLR type. One line per event. Adding an event = adding a line.
    private static readonly Dictionary<string, Type> ByName = new()
    {
        ["ParticipantActivated"] = typeof(ParticipantActivated),
        ["ParticipantCompleted"] = typeof(ParticipantCompleted),
        ["ParticipantConsented"] = typeof(ParticipantConsented),
        ["ParticipantCreated"] = typeof(ParticipantCreated),
        ["ParticipantEnrolled"] = typeof(ParticipantEnrolled),
        ["ParticipantLostToFollowUp"] = typeof(ParticipantLostToFollowUp),
        ["ParticipantScreenFailed"] = typeof(ParticipantScreenFailed),
        ["ParticipantScreeningStarted"] = typeof(ParticipantScreeningStarted),
        ["ParticipantWithdrawn"] = typeof(ParticipantWithdrawn),
        ["SiteActivated"] = typeof(SiteActivated),
        ["SiteClosed"] = typeof(SiteClosed),
        ["SiteCreated"] = typeof(SiteCreated),
        ["StudyActivated"] = typeof(StudyActivated),
        ["StudyArchived"] = typeof(StudyArchived),
        ["StudyClosed"] = typeof(StudyClosed),
        ["StudyCreated"] = typeof(StudyCreated),
        ["StudySuspended"] = typeof(StudySuspended),
        ["RoleAssigned"] = typeof(RoleAssigned),
        ["RoleAssignmentEnded"] = typeof(RoleAssignmentEnded),
        ["UserActivated"] = typeof(UserActivated),
        ["UserCreated"] = typeof(UserCreated),
        ["UserDeactivated"] = typeof(UserDeactivated),
        ["UserLocked"] = typeof(UserLocked),
        ["UserMfaEnrolled"] = typeof(UserMfaEnrolled),
        ["UserUnlocked"] = typeof(UserUnlocked),
    };

    private static readonly Dictionary<Type, string> ByType =
        ByName.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static readonly JsonSerializerOptions Options = BuildOptions();

    public static string NameOf(IDomainEvent e) =>
        ByType.TryGetValue(e.GetType(), out var name)
            ? name
            : throw new InvalidOperationException(
                $"Domain event {e.GetType().Name} is not registered in OutboxSerialization. Add it to the map.");

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