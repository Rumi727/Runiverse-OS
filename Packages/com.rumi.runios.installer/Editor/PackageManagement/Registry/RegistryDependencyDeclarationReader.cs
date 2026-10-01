#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;

namespace RuniOS.PackageManagement.Unity.Editor.Registry
{
    /// <summary>
    /// Decodes exact registry versions and explicit scoped-registry routing.<br/>
    /// 정확한 registry 버전과 명시적인 scoped registry 연결을 해석합니다.
    /// </summary>
    public sealed class RegistryDependencyDeclarationReader : IUpmDependencyDeclarationReader
    {
        /// <inheritdoc/>
        public string sourceKind => "registry";
        /// <inheritdoc/>
        public PackageRequirement Read(PackageId id, string declarationJson, UpmDeclarationContext context)
        {
            Dictionary<string, object?> declaration = JsonData.Parse(declarationJson);
            RegistryPackageSource source;
            if (JsonData.Optional(declaration, "url") is { } url)
            {
                url = AdapterData.HttpUrl(url);
                ScopedRegistryDefinition? registry = context.sources.registries.FirstOrDefault(x => x.url == url && x.Match(id) >= 0);
                if (registry is null && url != context.sources.defaultRegistryUrl)
                    registry = new ScopedRegistryDefinition(JsonData.Required(declaration, "name"), url,
                        JsonData.Array(declaration.TryGetValue("scopes", out object? scopes) ? scopes : null).Select(JsonData.String));
                source = new RegistryPackageSource(id, url, registry);
            }
            else source = context.sources.RegistrySource(id);
            return new PackageRequirement(id, source, new[] { new UpmVersionConstraint(JsonData.Required(declaration, "version")) }, context.location);
        }
    }
}
