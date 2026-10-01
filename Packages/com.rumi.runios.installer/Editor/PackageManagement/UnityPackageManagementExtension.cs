#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Providers;
using System.IO;
using System.Net.Http;
using UnityEngine;
using UnityEditor;
using RuniOS.PackageManagement.Unity.Editor.Internal;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;
using RuniOS.PackageManagement.Unity.Editor.State;
using RuniOS.PackageManagement.Unity.Editor.State.Internal;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Git;
using RuniOS.PackageManagement.Unity.Editor.Registry;
using RuniOS.PackageManagement.Unity.Editor.Builtin;
using RuniOS.PackageManagement.Unity.Editor.Manifest;
using RuniOS.PackageManagement.Unity.Editor.Execution;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Registers Git, registry and Unity environment capabilities without requiring another package.<br/>
    /// 다른 패키지 없이 Git, registry 및 Unity 환경 기능을 등록합니다.
    /// </summary>
    public sealed class UnityPackageManagementExtension : IPackageManagementExtension
    {
        static readonly HttpClient sharedClient = new() { Timeout = TimeSpan.FromMinutes(2) };
        readonly UpmSourceConfiguration sources;
        readonly HttpClient client;
        readonly IReadOnlyList<IUpmDependencyDeclarationReader>? readers;
        /// <summary>
        /// Reads current project registry routes and discovers declaration readers without installing packages.<br/>
        /// 패키지 설치 없이 현재 프로젝트 registry 연결을 읽고 선언 리더를 검색합니다.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UnityPackageManagementExtension() : this(CurrentSources(), sharedClient, DiscoverReaders()) { }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="sources">
        /// The immutable registry routing configuration.<br/>
        /// 불변 레지스트리 연결 설정입니다.
        /// </param>
        /// <param name="client">
        /// The caller-owned HTTP client; scope authentication handlers to their intended origins.<br/>
        /// 호출자가 소유하는 HTTP client이며 인증 handler를 대상 origin으로 제한합니다.
        /// </param>
        /// <param name="readers">
        /// Optional complete declaration-reader registrations; <see langword="null"/> uses the built-in readers.<br/>
        /// 선택적인 전체 선언 리더 등록이며 <see langword="null"/>이면 기본 리더를 사용합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UnityPackageManagementExtension(UpmSourceConfiguration sources, HttpClient client, IEnumerable<IUpmDependencyDeclarationReader>? readers = null)
        {
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.readers = readers is null ? null : AdapterData.List(readers);
        }
        /// <inheritdoc/>
        public void Register(PackageManagementBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            UpmMetadataReader metadata = new(sources, readers);
            builder.AddCatalog(new GitPackageCatalogProvider(metadata));
            builder.AddCatalog(new RegistryPackageCatalogProvider(metadata, client));
            builder.AddCatalog(new UnityBuiltinCatalogProvider(sources));
            builder.AddConstraintEvaluator(new GitReferenceEvaluator());
            builder.AddConstraintEvaluator(new UpmVersionEvaluator());
            builder.AddInstalledStateProvider(new UnityInstalledStateProvider(readers));
            builder.AddInstallationPlanner(new UnityManifestPlanningProvider());
            builder.AddOperationExecutor(new UnityManifestExecutor());
        }
        static IEnumerable<IUpmDependencyDeclarationReader> DiscoverReaders()
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IUpmDependencyDeclarationReader>()
                .Where(x => !x.IsAbstract && !x.IsInterface && !x.ContainsGenericParameters)
                .OrderBy(x => x.AssemblyQualifiedName ?? x.FullName ?? x.Name, StringComparer.Ordinal))
            {
                if (type.GetConstructor(Type.EmptyTypes) is null)
                    throw new ArgumentException($"Declaration reader '{type.FullName}' requires a public parameterless constructor.");
                yield return (IUpmDependencyDeclarationReader)(Activator.CreateInstance(type) ?? throw new InvalidOperationException("The declaration reader could not be constructed."));
            }
        }
        static UpmSourceConfiguration CurrentSources()
        {
            string project = Directory.GetParent(Application.dataPath)?.FullName ?? throw new IOException("The current Unity project directory is unavailable.");
            string path = Path.Combine(project, "Packages", "manifest.json");
            return UnityProjectFiles.Sources(JsonData.Parse(File.ReadAllText(path)));
        }
    }
}
