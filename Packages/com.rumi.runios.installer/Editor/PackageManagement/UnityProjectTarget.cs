#nullable enable
using System;
using System.IO;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Identifies a project directory without reading or changing package state.<br/>
    /// 패키지 상태를 읽거나 변경하지 않고 프로젝트 디렉터리를 식별합니다.
    /// </summary>
    public sealed class UnityProjectTarget : InstallationTarget
    {
        /// <summary>
        /// Identifies the editor-project observation capability.<br/>
        /// 에디터 프로젝트 관측 기능을 식별합니다.
        /// </summary>
        public const string targetKind = "unity:editor-project";
        /// <summary>
        /// Gets the normalized project location without verifying its existence.<br/>
        /// 존재를 확인하지 않고 정규화된 프로젝트 위치를 가져옵니다.
        /// </summary>
        public string projectPath => this.key;

        /// <summary>
        /// Normalizes a project path without requiring the directory to exist.<br/>
        /// 디렉터리의 존재를 요구하지 않고 프로젝트 경로를 정규화합니다.
        /// </summary>
        /// <param name="projectPath">
        /// The project path to normalize.<br/>
        /// 정규화할 프로젝트 경로입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="projectPath"/> is <see langword="null"/>.<br/>
        /// <paramref name="projectPath"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the path is empty or invalid.<br/>
        /// 경로가 비어 있거나 유효하지 않으면 발생합니다.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// May be thrown when the path format is unsupported.<br/>
        /// 경로 형식이 지원되지 않으면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// May be thrown when the path exceeds platform limits.<br/>
        /// 경로가 플랫폼 제한을 초과하면 발생할 수 있습니다.
        /// </exception>
        public UnityProjectTarget(string projectPath) : base(targetKind, Normalize(projectPath)) { }

        static string Normalize(string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A project path is required.", nameof(path));
            string fullPath = Path.GetFullPath(path);
            string? root = Path.GetPathRoot(fullPath);
            return fullPath.Length == root?.Length ? fullPath : fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
