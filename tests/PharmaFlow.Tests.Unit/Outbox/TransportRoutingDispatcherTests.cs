using System.Net;

using Mediator;

using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Infrastructure.Outbox;

namespace PharmaFlow.Tests.Unit.Outbox;

// PFL-065 strangler lever. The router reads OutboxOptions.Transport per call, so flipping the flag
// on the shared (singleton) options changes the next dispatch with NO new instance and no restart —
// the no-redeploy rollback lever PFL-066's drill pulls.
public class TransportRoutingDispatcherTests
{
    private static readonly DateTimeOffset Occurred =
        new(2026, 6, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Default_InProc_routes_to_the_in_proc_dispatcherAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var (router, publisher, handler) = Build(new OutboxOptions()); // default = InProc

        await router.DispatchAsync(Event(), Guid.NewGuid(), ct);

        Assert.Single(publisher.Published);
        Assert.Null(handler.Request); // HTTP transport never touched
    }

    [Fact]
    public async Task Http_flag_routes_to_the_http_dispatcherAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var (router, publisher, handler) = Build(
            new OutboxOptions { Transport = OutboxOptions.IntegrationTransport.Http });

        await router.DispatchAsync(Event(), Guid.NewGuid(), ct);

        Assert.NotNull(handler.Request);
        Assert.Empty(publisher.Published); // in-proc never touched
    }

    [Fact]
    public async Task Flipping_the_flag_at_runtime_changes_the_next_dispatch_without_a_new_instanceAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = new OutboxOptions(); // starts InProc
        var (router, publisher, handler) = Build(options);

        await router.DispatchAsync(Event(), Guid.NewGuid(), ct);
        Assert.Single(publisher.Published);
        Assert.Null(handler.Request);

        options.Transport = OutboxOptions.IntegrationTransport.Http; // the drill flips it live

        await router.DispatchAsync(Event(), Guid.NewGuid(), ct);
        Assert.NotNull(handler.Request);    // the next dispatch went HTTP...
        Assert.Single(publisher.Published); // ...and added no further in-proc publish
    }

    private static StudyCreatedIntegrationEvent Event() => new(Guid.NewGuid(), Occurred);

    private static (TransportRoutingDispatcher Router, RecordingPublisher Publisher, StubHandler Handler) Build(
        OutboxOptions options)
    {
        var publisher = new RecordingPublisher();
        var handler = new StubHandler();
        var http = new HttpIntegrationEventDispatcher(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        var router = new TransportRoutingDispatcher(
            new InProcIntegrationEventDispatcher(publisher), http, options);
        return (router, publisher, handler);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class RecordingPublisher : IPublisher
    {
        public List<INotification> Published { get; } = [];

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification);
            return ValueTask.CompletedTask;
        }

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add((INotification)notification);
            return ValueTask.CompletedTask;
        }
    }
}