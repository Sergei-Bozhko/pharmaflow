using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

using Mediator;

using PharmaFlow.Application.Modules.Studies.Contracts;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace PharmaFlow.Tests.Architecture;

// PFL-056: the PFL-053 boundary, enforced by the build. Real teeth are the dependency
// rules (R1/R2/R4); R3 is a narrowed visibility check (handlers + module impls only —
// validators/DTOs/DbContext interfaces are public by DI/STJ necessity, not a leak).
public class ModuleBoundaryTests
{
    // Single source of truth for the namespace convention — add a module = edit here.
    private const string App = "PharmaFlow.Application";
    private const string Studies = App + ".Modules.Studies";
    private const string Sites = App + ".Modules.Sites";
    private const string Common = App + ".Common";

    // Fully qualified: this project's namespace tail (…Tests.Architecture) otherwise
    // shadows ArchUnitNET's Architecture type.
    private static readonly ArchUnitNET.Domain.Architecture Arch =
        new ArchLoader().LoadAssemblies(typeof(IStudiesModule).Assembly).Build();

    // regex matching a namespace prefix and its descendants
    private static string Tree(string root) => $"^{root.Replace(".", "\\.")}($|\\..*)";

    // R1 — no cross-module internals
    [Fact]
    public void Sites_must_not_depend_on_Studies_internals() =>
        Types().That().ResideInNamespaceMatching(Tree(Sites))
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(Tree(Studies + ".Internal")))
            .Check(Arch);

    [Fact]
    public void Studies_must_not_depend_on_Sites_internals() =>
        Types().That().ResideInNamespaceMatching(Tree(Studies))
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(Tree(Sites + ".Internal")))
            .Check(Arch);

    // R2 — cross-module dependencies only via Contracts
    [Fact]
    public void Sites_uses_Studies_only_via_Contracts() =>
        Types().That().ResideInNamespaceMatching(Tree(Sites))
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(Tree(Studies))
                .And().DoNotResideInNamespaceMatching(Tree(Studies + ".Contracts")))
            .Check(Arch);

    // R3 (narrowed) — handlers + module impls under .Internal must be non-public
    [Fact]
    public void Internal_handlers_are_not_public() =>
        Classes().That().ResideInNamespaceMatching(@".*\.Modules\..*\.Internal($|\..*)")
            .And().HaveNameEndingWith("Handler")
            .Should().NotBePublic()
            .Check(Arch);

    [Fact]
    public void Internal_module_impls_are_not_public() =>
        Classes().That().ResideInNamespaceMatching(@".*\.Modules\..*\.Internal($|\..*)")
            .And().HaveNameEndingWith("Module")
            .Should().NotBePublic()
            .Check(Arch);

    // R4 — explicit: no reaching past a contract into another module's DbContext
    [Fact]
    public void Sites_must_not_use_Studies_DbContext() =>
        Types().That().ResideInNamespaceMatching(Tree(Sites))
            .Should().NotDependOnAny(Types().That().HaveNameContaining("IStudiesDbContext"))
            .Check(Arch);

    [Fact]
    public void Studies_must_not_use_Sites_DbContext() =>
        Types().That().ResideInNamespaceMatching(Tree(Studies))
            .Should().NotDependOnAny(Types().That().HaveNameContaining("ISitesDbContext"))
            .Check(Arch);

    // R5 (optional, cheap) — Common is module-agnostic
    [Fact]
    public void Common_must_not_depend_on_any_module() =>
        Types().That().ResideInNamespaceMatching(Tree(Common))
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(Tree(App + ".Modules")))
            .Check(Arch);

    // regex matching any namespace whose tail contains a `.Contracts` segment
    private const string ContractsSegment = @".*\.Contracts($|\..*)";

    // R6 — integration events (INotification) live in a module's Contracts, never Internal.
    // The S5 contracts rule applied to events: a consumer can only reference what's in Contracts.
    [Fact]
    public void Integration_events_reside_in_Contracts() =>
        Types().That()
            .ImplementInterface(typeof(INotification))
            .And().ResideInNamespaceMatching(Tree(App + ".Modules"))
            .Should().ResideInNamespaceMatching(ContractsSegment)
            .Check(Arch);

    // R7 — a subscriber (INotificationHandler) reaches the producer module only via Contracts,
    // never its Internal. Focused restatement of R2 on the event path (the producer here is Studies).
    [Fact]
    public void Notification_handlers_reach_Studies_only_via_Contracts() =>
        Types().That()
            .ImplementInterface(typeof(INotificationHandler<>))
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(Tree(Studies))
                .And().DoNotResideInNamespaceMatching(Tree(Studies + ".Contracts")))
            .Check(Arch);
}