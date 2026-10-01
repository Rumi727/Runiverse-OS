#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;

namespace RuniOS.PackageManagement.Unity.Editor.Git
{
    /// <summary>
    /// Decodes Git declarations, inheriting the parent commit for repository-relative dependencies.<br/>
    /// Git 선언을 해석하고 저장소 상대 의존성에는 부모 commit을 적용합니다.
    /// </summary>
    public sealed class GitDependencyDeclarationReader : IUpmDependencyDeclarationReader
    {
        /// <inheritdoc/>
        public string sourceKind => "git";
        /// <inheritdoc/>
        public PackageRequirement Read(PackageId id, string declarationJson, UpmDeclarationContext context)
        {
            Dictionary<string, object?> declaration = JsonData.Parse(declarationJson);
            GitCommitArtifact? parent = context.parent.artifacts.OfType<GitCommitArtifact>().SingleOrDefault();
            string repository = JsonData.Optional(declaration, "repository") ?? parent?.source.repositoryUrl ??
                throw new FormatException("A repository URL is required outside a Git parent.");
            GitPackageSource source = new(repository, JsonData.Required(declaration, "path"));
            string? reference = JsonData.Optional(declaration, "reference");
            PackageConstraint constraint = reference is null && parent is not null && source.repositoryUrl == parent.source.repositoryUrl ?
                new ExactRevisionConstraint(parent.commit) : new GitReferenceConstraint(reference ?? "HEAD");
            return new PackageRequirement(id, source, new[] { constraint }, context.location);
        }
    }
}
