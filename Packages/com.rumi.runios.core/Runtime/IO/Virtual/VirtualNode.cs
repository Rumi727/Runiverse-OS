#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Provides common state and lifecycle operations for virtual file-system nodes.<br/>
    /// 가상 파일 시스템 노드의 공통 상태와 수명 주기 작업을 제공합니다.
    /// </summary>
    public abstract class VirtualNode
    {
        /// <summary>
        /// Initializes a new <see cref="VirtualNode"/> instance and marks directory instances as their own root until attached.<br/>
        /// 새 <see cref="VirtualNode"/> 인스턴스를 초기화하고, 디렉터리 인스턴스는 연결 전까지 자기 자신을 루트로 표시합니다.
        /// </summary>
        protected VirtualNode() => root = this as VirtualDirectoryBase;

        /// <summary>
        /// Gets a value indicating whether this node is the root directory node.<br/>
        /// 이 노드가 루트 디렉터리 노드인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        [MemberNotNullWhen(true, nameof(fullPath))]
        public bool isRoot => this is VirtualDirectoryBase && parent == null;

        /// <summary>
        /// Gets the top-level root directory of the virtual file system that contains this node.<br/>
        /// 이 노드가 속한 가상 파일 시스템의 최상위 루트 디렉터리를 가져옵니다.
        /// </summary>
        public VirtualDirectoryBase? root { get; private set; }

        /// <summary>
        /// Gets the parent directory of this node, or <see langword="null"/> when the node is not attached.<br/>
        /// 이 노드의 부모 디렉터리를 가져오며, 노드가 연결되어 있지 않으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        public VirtualDirectoryBase? parent { get; private set; }

        /// <summary>
        /// Gets the name assigned by the parent directory, or <see langword="null"/> when the node is not attached.<br/>
        /// 부모 디렉터리가 지정한 이름을 가져오며, 노드가 연결되어 있지 않으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        public string? name { get; private set; }

        /// <summary>
        /// Gets the full path of this node, or <see langword="null"/> when the node is detached.<br/>
        /// 이 노드의 전체 경로를 가져오며, 노드가 분리되어 있으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        public RuniPath? fullPath => isDetached ? null : parent?.fullPath + name;

        /// <summary>
        /// Gets the metadata associated with this node.<br/>
        /// 이 노드와 연결된 메타 데이터를 가져옵니다.
        /// </summary>
        public IOMetaData metaData { get; private set; } = new IOMetaData
        {
            name = null,
            creationTime = DateTime.UtcNow,
            lastWriteTime = DateTime.UtcNow
        };

        /// <summary>
        /// Gets a value indicating whether this node is a directory.<br/>
        /// 이 노드가 디렉터리인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isDirectory => this is VirtualDirectoryBase;

        /// <summary>
        /// Gets a value indicating whether this node is attached to a parent directory.<br/>
        /// 이 노드가 부모 디렉터리에 연결되어 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        [MemberNotNullWhen(true, nameof(root), nameof(parent), nameof(name), nameof(fullPath))]
        public bool isAttached => parent != null;

        /// <summary>
        /// Gets a value indicating whether this node is neither a root directory nor attached to a parent directory.<br/>
        /// 이 노드가 루트 디렉터리도 아니고 부모 디렉터리에 연결되어 있지도 않은지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isDetached => !isRoot && !isAttached;

        /// <summary>
        /// Gets a value indicating whether this node has been deleted and can no longer be used.<br/>
        /// 이 노드가 삭제되어 더 이상 사용할 수 없는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isDeleted { get; private set; } = false;

        internal void OnAttached(string name, VirtualDirectoryBase parent)
        {
            ThrowIfDeletedException();
            ThrowIfAttachedException();

            root = parent.root;
            this.parent = parent;
            this.name = name;

            metaData = metaData with
            {
                name = name
            };

            if (this is VirtualDirectoryBase directory)
                directory.InvalidateCache();
        }

        /// <summary>
        /// Detaches this node from its parent directory.<br/>
        /// 이 노드를 부모 디렉터리에서 분리합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this node is not attached.<br/>
        /// 이 노드가 연결되어 있지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this node has been deleted.<br/>
        /// 이 노드가 삭제된 경우 발생합니다.
        /// </exception>
        public void Detach()
        {
            ThrowIfDeletedException();
            ThrowIfNotAttachedException();

            parent.OnDetachChild(this);

            root = this as VirtualDirectoryBase;
            parent = null;
            name = null;

            if (this is VirtualDirectoryBase directory)
                directory.InvalidateCache();
        }

        /// <summary>
        /// Deletes this node and detaches it from its parent if needed.<br/>
        /// 이 노드를 삭제하고 필요한 경우 부모에서 분리합니다.
        /// </summary>
        public void Delete()
        {
            ThrowIfDeletedException();

            OnDelete();

            if (isAttached)
                Detach();

            isDeleted = true;
        }

        /// <summary>
        /// Performs type-specific cleanup when this node is deleted.<br/>
        /// 이 노드가 삭제될 때 타입별 정리 작업을 수행합니다.
        /// </summary>
        public abstract void OnDelete();

        /// <summary>
        /// Returns this node as a directory when possible.<br/>
        /// 가능한 경우 이 노드를 디렉터리로 반환합니다.
        /// </summary>
        /// <returns>
        /// This node as a <see cref="VirtualDirectoryBase"/> if it is a directory; otherwise, <see langword="null"/>.<br/>
        /// 이 노드가 디렉터리이면 <see cref="VirtualDirectoryBase"/>로 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public VirtualDirectoryBase? AsDirectory() => this as VirtualDirectoryBase;

        /// <summary>
        /// Returns this node as a file when possible.<br/>
        /// 가능한 경우 이 노드를 파일로 반환합니다.
        /// </summary>
        /// <returns>
        /// This node as a <see cref="VirtualFileBase"/> if it is a file; otherwise, <see langword="null"/>.<br/>
        /// 이 노드가 파일이면 <see cref="VirtualFileBase"/>로 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public VirtualFileBase? AsFile() => this as VirtualFileBase;

        /// <summary>
        /// Throws when <see cref="isAttached"/> is <see langword="true"/>.<br/>
        /// <see cref="isAttached"/>가 <see langword="true"/>이면 예외를 발생시킵니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this node is already attached.<br/>
        /// 이 노드가 이미 연결되어 있는 경우 발생합니다.
        /// </exception>
        public void ThrowIfAttachedException()
        {
            if (isAttached)
                throw new InvalidOperationException( /* TODO 예외 메시지 적기 */);
        }

        /// <summary>
        /// Throws when <see cref="isAttached"/> is <see langword="false"/>.<br/>
        /// <see cref="isAttached"/>가 <see langword="false"/>이면 예외를 발생시킵니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this node is not attached.<br/>
        /// 이 노드가 연결되어 있지 않은 경우 발생합니다.
        /// </exception>
        [MemberNotNull(nameof(root), nameof(parent), nameof(name), nameof(fullPath))]
        public void ThrowIfNotAttachedException()
        {
            if (!isAttached)
                throw new InvalidOperationException( /* TODO 예외 메시지 적기 */);
        }

        /// <summary>
        /// Throws when <see cref="isDeleted"/> is <see langword="true"/>.<br/>
        /// <see cref="isDeleted"/>가 <see langword="true"/>이면 예외를 발생시킵니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this node has been deleted.<br/>
        /// 이 노드가 삭제된 경우 발생합니다.
        /// </exception>
        public void ThrowIfDeletedException()
        {
            if (isDeleted)
                throw new ObjectDisposedException(GetType().Name, "/* TODO 예외 메시지 적기 */");
        }

        /// <summary>
        /// Throws when the specified node name is empty or contains a directory separator.<br/>
        /// 지정된 노드 이름이 비어 있거나 디렉터리 구분자를 포함하면 예외를 발생시킵니다.
        /// </summary>
        /// <param name="name">
        /// The node name to validate.<br/>
        /// 검사할 노드 이름입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="name"/> is invalid.<br/>
        /// <paramref name="name"/>이 유효하지 않은 경우 발생합니다.
        /// </exception>
        public static void ThrowIfInvalidNodeName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(RuniPath.directorySeparatorChars) >= 0)
                throw new InvalidOperationException($"The node name '{name}' contains invalid characters.");
        }

        /// <summary>
        /// Throws when the specified file name is empty or contains a directory separator.<br/>
        /// 지정된 파일 이름이 비어 있거나 디렉터리 구분자를 포함하면 예외를 발생시킵니다.
        /// </summary>
        /// <param name="name">
        /// The file name to validate.<br/>
        /// 검사할 파일 이름입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="name"/> is invalid.<br/>
        /// <paramref name="name"/>이 유효하지 않은 경우 발생합니다.
        /// </exception>
        public static void ThrowIfInvalidFileName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(RuniPath.directorySeparatorChars) >= 0)
                throw new InvalidOperationException($"The file name '{name}' contains invalid characters.");
        }
    }
}
