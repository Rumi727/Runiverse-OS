#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Providers;

namespace RuniOS.Installer.Editor
{
    /// <summary>
    /// Creates frontend services from explicitly implemented extension entry points.<br/>
    /// 명시적으로 구현한 확장 진입점에서 프런트엔드 서비스를 생성합니다.
    /// </summary>
    public static class InstallerPackageManagement
    {
        /// <summary>
        /// Discovers concrete parameterless extensions and validates their registrations without installation.<br/>
        /// 설치 없이 매개변수 없는 구체적인 확장을 검색하고 등록을 검증합니다.
        /// </summary>
        /// <returns>
        /// A provider registry or registration diagnostics without a registry.<br/>
        /// 프로바이더 레지스트리를 반환하거나 레지스트리 없이 등록 진단을 반환합니다.
        /// </returns>
        public static PackageResult<PackageManagementServices> CreateServices()
        {
            PackageManagementBuilder builder = new();
            List<PackageDiagnostic> diagnostics = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IPackageManagementExtension>()
                .Where(x => !x.IsAbstract && !x.IsInterface && !x.ContainsGenericParameters)
                .OrderBy(x => x.AssemblyQualifiedName ?? x.FullName ?? x.Name, StringComparer.Ordinal))
            {
                if (type.GetConstructor(Type.EmptyTypes) is null)
                {
                    diagnostics.Add(Error(type, "A public parameterless extension constructor is required."));
                    continue;
                }
                try
                {
                    if (Activator.CreateInstance(type) is not IPackageManagementExtension extension)
                    {
                        diagnostics.Add(Error(type, "The extension entry point could not be created."));
                        continue;
                    }
                    builder.AddExtension(extension);
                }
                catch (Exception exception)
                {
                    diagnostics.Add(Error(type, exception.GetBaseException().Message));
                }
            }
            PackageResult<PackageManagementServices> result = builder.Build();
            diagnostics.AddRange(result.diagnostics);
            return new PackageResult<PackageManagementServices>(diagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error)
                ? null : result.value, diagnostics);
        }

        static PackageDiagnostic Error(Type type, string message) => new("installer:extension-registration-failed",
            PackageDiagnosticSeverity.Error, PackageDiagnosticPhase.Registration, $"{type.FullName}: {message}");
    }
}
