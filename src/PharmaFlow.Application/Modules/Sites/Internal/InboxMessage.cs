namespace PharmaFlow.Application.Modules.Sites.Internal;

public sealed class InboxMessage
{
    public Guid MessageId { get; set; }
    public DateTimeOffset RecievedAt { get; set; }

    private InboxMessage() { }
    public InboxMessage(Guid messageId, DateTimeOffset recievedAt)
    {
        MessageId = messageId;
        RecievedAt = recievedAt;
    }
}