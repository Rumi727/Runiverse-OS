using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace RuniOS.APIBridge
{
    public record struct BridgeGenerationData
    (
        string bridgeNamespace,
        ImmutableArray<string> targetAssemblies,
        INamedTypeSymbol targetSymbol,
        ImmutableArray<string> includeMembers,
        ImmutableArray<string> excludeMembers,
        bool forceStatic,
        bool skipConstructors,
        ImmutableHashSet<int> excludeConstructors,
        bool includePublicMember,
        bool onlyByMyself
    );
}