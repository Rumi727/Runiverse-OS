#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;
using System.Threading;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RuniOS.Editor
{
    /// <summary>
    /// Exposes a folder of a package that the Package Manager manages as a read-only provider.<br/>
    /// 패키지 관리자가 관리하는 패키지의 폴더를 읽기 전용 프로바이더로 제공합니다.
    /// </summary>
    /// <remarks>
    /// Packages such as registry or Git packages are restored by the Package Manager, so changes made to their folders can be lost at any time.<br/>
    /// This provider does not implement <see cref="IWritableIOProvider"/>, so the folder cannot be changed through it.
    /// <br/><br/>
    /// 레지스트리나 Git 패키지 같은 패키지는 패키지 관리자가 되돌리므로, 그 폴더에 한 변경은 언제든 사라질 수 있습니다.<br/>
    /// 이 프로바이더는 <see cref="IWritableIOProvider"/>를 구현하지 않으므로, 이 프로바이더로는 폴더를 바꿀 수 없습니다.
    /// </remarks>
    public sealed class PackageIOProvider : IPhysicalBackedIOProvider
    {
        /// <summary>
        /// Gets the package that owns the folder.<br/>
        /// 폴더를 가진 패키지를 가져옵니다.
        /// </summary>
        public PackageInfo package { get; }

        /// <summary>
        /// Gets the physical root path used by this provider.<br/>
        /// 이 프로바이더가 기준으로 사용하는 물리 루트 경로를 가져옵니다.
        /// </summary>
        public PhysicalPath targetPath => physicalProvider.targetPath;

        /// <inheritdoc/>
        public bool isIndependent => false;

        /// <inheritdoc/>
        public bool isThreadSafe => true;

        readonly PhysicalIOProvider physicalProvider;

        /// <summary>
        /// Initializes a new <see cref="PackageIOProvider"/> for the specified folder of <paramref name="package"/>.<br/>
        /// <paramref name="package"/>의 지정한 폴더에 대한 새 <see cref="PackageIOProvider"/>를 초기화합니다.
        /// </summary>
        /// <param name="package">
        /// The package that owns the folder.<br/>
        /// 폴더를 가진 패키지입니다.
        /// </param>
        /// <param name="targetPath">
        /// The physical root path exposed by this provider.<br/>
        /// 이 프로바이더가 노출하는 물리 루트 경로입니다.
        /// </param>
        /// <param name="sandboxPolicy">
        /// The sandbox policy applied before physical file-system access.<br/>
        /// 실제 파일 시스템 접근 전에 적용할 샌드박스 정책입니다.
        /// </param>
        public PackageIOProvider(PackageInfo package, PhysicalPath targetPath, SandboxPolicy sandboxPolicy = SandboxPolicy.BlockLinks)
        {
            this.package = package;
            physicalProvider = new PhysicalIOProvider(targetPath, sandboxPolicy);
        }

        /// <inheritdoc/>
        public PhysicalPath GetPhysicalPath(RuniPath path) => physicalProvider.GetPhysicalPath(path);

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is PackageIOProvider otherPackage && physicalProvider.IsSameTarget(otherPackage.physicalProvider);

        /// <inheritdoc/>
        public UniTask<bool> DirectoryExists(RuniPath path, CancellationToken cancellationToken = default) => physicalProvider.DirectoryExists(path, cancellationToken);

        /// <inheritdoc/>
        public UniTask<bool> FileExists(RuniPath path, CancellationToken cancellationToken = default) => physicalProvider.FileExists(path, cancellationToken);

        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default) => physicalProvider.GetEntry(path, cancellationToken);

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default) => physicalProvider.EnumerateEntries(path, recursive, cancellationToken);

        /// <inheritdoc/>
        public UniTask<Stream> OpenRead(RuniPath path, CancellationToken cancellationToken = default) => ((IIOProvider)physicalProvider).OpenRead(path, cancellationToken);

        /// <inheritdoc/>
        public UniTask<byte[]> ReadAllBytes(RuniPath path, CancellationToken cancellationToken = default) => physicalProvider.ReadAllBytes(path, cancellationToken);

        /// <inheritdoc/>
        public UniTask<string> ReadAllText(RuniPath path, CancellationToken cancellationToken = default) => physicalProvider.ReadAllText(path, cancellationToken);

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLines(RuniPath path, CancellationToken cancellationToken = default) => physicalProvider.ReadLines(path, cancellationToken);

        void IDisposable.Dispose() { }
    }
}
