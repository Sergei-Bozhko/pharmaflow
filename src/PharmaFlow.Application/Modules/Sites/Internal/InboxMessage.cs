namespace PharmaFlow.Application.Modules.Sites.Internal;

public sealed class InboxMessage
{
    public Guid MessageId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }

    private InboxMessage() { }
    public InboxMessage(Guid messageId, DateTimeOffset receivedAt)
    {
        MessageId = messageId;
        ReceivedAt = receivedAt;
    }
}